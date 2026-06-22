using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EasyFastTcpSerialWrapper;

public delegate void RouteDataReceived(byte[] data, int count);

public class FastEasyTcpClient
{
    public event RouteDataReceived DataReceived;


    private const int MAX_TCP_MESSAGE = 1500;
    private const int RECONNECT_DELAY_MS = 1000;


    public FastEasyTcpClient(string hostName, int port, bool nagleDelay = false)
    {
        _hostName = hostName;
        _port = port;
        _nagleDelay = nagleDelay;
        _enable = false;
    }


    public void Connect()
    {
        if (_enable) return;

        _enable = true;
        _cts = new CancellationTokenSource();

        // Запускаем единый фоновый поток управления подключением
        Task.Run(() => ConnectionLoopAsync(_cts.Token));
    }


    public void Disconnect()
    {
        _enable = false;
        _cts?.Cancel();
        CloseConnection();
    }


    // Потокобезопасная отправка
    public void Send(byte[] data, int pos, int count)
    {
        if (!_enable || data == null || pos < 0 || (pos + count) > data.Length)
        {
            return;
        }

        for (int i = pos; i < (pos + count); i++)
        {
            _sendQueue.Enqueue(data[i]);
        }

        if (count > 0)
        {
            // Триггерим асинхронную отправку накопленных байт
            Task.Run(() => ProcessWriteQueueAsync());
        }
    }


    public void SetNagleDelay(bool delay)
    {
        _nagleDelay = delay;
        if (_client != null)
        {
            _client.NoDelay = !_nagleDelay;
        }
    }


    // Главный цикл автоподключения
    private async Task ConnectionLoopAsync(CancellationToken token)
    {
        while (_enable && !token.IsCancellationRequested)
        {
            try
            {
                _client = new TcpClient();
                _client.NoDelay = !_nagleDelay;

                // Асинхронное подключение (совместимо с .NET Framework 4.8)
                await Task.Factory.FromAsync
                (
                    _client.BeginConnect
                    , _client.EndConnect
                    , _hostName
                    , _port
                    , null
                ).ConfigureAwait(false);

                _stream = _client.GetStream();

                // Запускаем чтение. Ждем его завершения (признак обрыва связи)
                await ReadLoopAsync(token).ConfigureAwait(false);
            }
            catch
            {
                // Ошибка подключения или обрыв связи
                CloseConnection();
            }

            // Пауза перед следующей попыткой переподключения
            if (_enable && !token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(RECONNECT_DELAY_MS, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }


    // Асинхронное чтение данных из сокета
    private async Task ReadLoopAsync(CancellationToken token)
    {
        byte[] buffer = new byte[MAX_TCP_MESSAGE];
        NetworkStream stream = _stream;

        while (_enable && _client.Connected && !token.IsCancellationRequested)
        {
            int recLength = 0;
            try
            {
                recLength = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
            }
            catch
            {
                break; // Ошибка чтения — выходим на переподключение
            }

            if (recLength == 0)
            {
                break; // Сервер закрыл соединение
            }

            DataReceived?.Invoke(buffer, recLength);
        }
    }


    // Потокобезопасная асинхронная отправка
    private async Task ProcessWriteQueueAsync()
    {
        // Предотвращаем одновременную запись из разных потоков
        if (!await _writeSemaphore.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            NetworkStream stream = _stream;
            if (stream == null || _client == null || !_client.Connected)
            {
                ClearSendQueue();
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
            CloseConnection();
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }


    private void CloseConnection()
    {
        try
        {
            _stream?.Dispose();
        }
        catch { }
        try
        {
            _client?.Close();
        }
        catch { }

        _stream = null;
        _client = null;
        ClearSendQueue();
    }


    private void ClearSendQueue()
    {
        // Безопасная очистка очереди для .NET Framework 4.8.1
        while (_sendQueue.TryDequeue(out _))
        {
        }
    }


    private readonly string _hostName;
    private readonly int _port;
    private readonly ConcurrentQueue<byte> _sendQueue = new ConcurrentQueue<byte>();
    private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1, 1);
    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;
    private bool _enable;
    private bool _nagleDelay;
}
