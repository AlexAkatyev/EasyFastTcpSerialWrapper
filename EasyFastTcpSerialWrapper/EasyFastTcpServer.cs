using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EasyFastTcpSerialWrapper;


public class TcpServer
{
    public delegate void TcpServerReceiveHandler(byte[] data, int length);
    public event TcpServerReceiveHandler? DataReceivedNotify;

    private const int MAX_TCP_MESSAGE = 1500;

    public TcpServer(IPAddress address, int port, bool nagleDelay = false)
    {
        _nagleDelay = nagleDelay;
        _server = new TcpListener(address, port);
        _enable = false;
    }


    public void SetNagleDelay(bool delay)
    {
        _nagleDelay  = delay;
        if (_currentClient != null)
        {
            _currentClient.NoDelay = !_nagleDelay;
        }
    }


    public bool Start()
    {
        if (_enable)
        {
            return true;
        }
        try
        {
            _server.Start();
            _enable = true;
            _cts = new CancellationTokenSource();

            // Запускаем асинхронный цикл прослушивания портов
            _ = Task.Run(() => AcceptClientsAsync(_cts.Token));
            return true;
        }
        catch
        {
            return false;
        }
    }


    public void Stop()
    {
        _enable = false;
        _cts?.Cancel();
        _server.Stop();
        DisconnectCurrentClient();
    }


    public void Send(byte[] data, int pos, int count)
    {
        if (!_enable
            || data == null
            || pos < 0
            || (pos + count) > data.Length)
        {
            return;
        }
        for (int i = pos; i < (pos + count); i++)
        {
            _sendQueue.Enqueue(data[i]);
        }
        if (count > 0)
        {
            // Триггерим отправку данных в фоне без блокировок
            _ = Task.Run(() => ProcessWriteQueueAsync());
        }
    }


    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (_enable && !token.IsCancellationRequested)
        {
            try
            {
                // Исправлено для .NET Framework 4.8.1 (убран аргумент token)
                TcpClient incomingClient = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
                incomingClient.NoDelay = !_nagleDelay;

                // Сценарий Single-Client: Если кто-то уже подключен,
                // принудительно закрываем старую сессию.
                if (_currentClient != null)
                {
                    DisconnectCurrentClient();
                }

                _currentClient = incomingClient;
                _stream = incomingClient.GetStream();

                // Запускаем чтение для одного конкретного клиента
                _ = Task.Run(() => ReadHandleClientAsync(incomingClient, token), token);
            }
            catch (ObjectDisposedException)
            {
                // Сюда мы зайдем, когда вызовем метод Stop() и сервер закроется. 
                // Это нормальное поведение для завершения цикла в .NET Framework.
                break;
            }
            catch (Exception)
            {
                // Перестраховка на случай непредвиденных ошибок сокета
                if (!_enable) break;
            }
        }
    }


    // Асинхронное чтение без таймеров
    private async Task ReadHandleClientAsync(TcpClient client, CancellationToken token)
    {
        byte[] recBuffer = new byte[MAX_TCP_MESSAGE];
        NetworkStream? stream = _stream;

        if (stream == null) return;

        while
        (
            _enable
            && client.Connected
            && !token.IsCancellationRequested
        )
        {
            try
            {
                // Асинхронное чтение: поток освобождается, пока нет данных в сокете
                int recLength = await stream.ReadAsync(recBuffer, 0, recBuffer.Length, token).ConfigureAwait(false);

                if (recLength == 0)
                {
                    // Клиент корректно закрыл соединение
                    break;
                }

                DataReceivedNotify?.Invoke(recBuffer, recLength);
            }
            catch
            {
                break; // Ошибка чтения или клиент отключился жестко
            }
        }

        if (_currentClient == client)
        {
            DisconnectCurrentClient();
        }
    }


    // Потокобезопасная асинхронная отправка пакетов пакетами до 1500 байт
    private async Task ProcessWriteQueueAsync()
    {
        // Используем семафор, чтобы только один поток отправлял данные в сокет в конкретный момент времени
        if (!await _writeSemaphore.WaitAsync(0).ConfigureAwait(false))
        {
            return; // Запись уже выполняется другим потоком, он сам заберет данные из очереди
        }

        try
        {
            NetworkStream? stream = _stream;
            if (stream == null || _currentClient == null || !_currentClient.Connected)
            {
                while (_sendQueue.TryDequeue(out _))
                {
                    // Очищаем очередь, извлекая все элементы в никуда
                }
                return;
            }

            while (!_sendQueue.IsEmpty)
            {
                int chunkSize = Math.Min(MAX_TCP_MESSAGE, _sendQueue.Count);
                if (chunkSize == 0) break;

                byte[] toSend = new byte[chunkSize];
                for (int i = 0; i < chunkSize; i++)
                {
                    if (_sendQueue.TryDequeue(out byte b))
                    {
                        toSend[i] = b;
                    }
                }

                await stream.WriteAsync(toSend, 0, toSend.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            DisconnectCurrentClient();
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }


    private void DisconnectCurrentClient()
    {
        try
        {
            _stream?.Dispose();
            _currentClient?.Dispose();
        }
        catch { }
        finally
        {
            _stream = null;
            _currentClient = null;
            while (_sendQueue.TryDequeue(out _))
            {
                // Очищаем очередь, извлекая все элементы в никуда
            }
        }
    }


    private readonly TcpListener _server;
    private TcpClient? _currentClient;
    private NetworkStream _stream;
    private readonly ConcurrentQueue<byte> _sendQueue = new();
    private bool _enable;
    private bool _nagleDelay = false;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
}
