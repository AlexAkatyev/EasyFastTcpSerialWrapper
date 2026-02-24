using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EasyFastTcpSerialWrapper;


public class TcpServer
{
    public delegate void TcpServerReceiveHandler(byte[] data, int length);
    private const int TIME_OUT_WAIT = 0;
    private const int TIME_OUT_ACCEPT = 1000;
    private const int TIME_OUT_WRITE = 100;
    private const int MAX_TCP_MESSAGE = 1500;

    public TcpServer(IPAddress address, int port)
    {
        _server = new TcpListener(address, port);
        _acceptTimer = new Timer
        (
            new TimerCallback(acceptClient)
            , null
            , Timeout.Infinite
            , Timeout.Infinite
        );
        _writeTimer = new Timer
        (
            new TimerCallback(write)
            , null
            , Timeout.Infinite
            , Timeout.Infinite
        );
        _enable = false;
    }


    public bool Start()
    {
        bool error = false;
        try
        {
            _server.Start();
            _enable = true;
            _acceptTimer.Change(TIME_OUT_WAIT, TIME_OUT_ACCEPT);
        }
        catch
        {
            error = true;
        }
        return !error;
    }


    public void Stop()
    {
        _acceptTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _enable = false;
        _server.Stop();
    }


    public void Send(byte[] data, int pos, int count)
    {
        if (!_enable
            || pos >= data.Length
            || (pos + count) > data.Length)
        {
            return;
        }
        for (int i = pos; i < (pos + count); i++)
        {
            _sendData.Add(data[i]);
        }
        if (count > 0)
        {
            _writeTimer.Change(0, TIME_OUT_WRITE);
        }
    }


    public event TcpServerReceiveHandler? DataReceivedNotify;


    private async void acceptClient(object obj)
    {
        #if DEBUG
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);
        ThreadPool.GetAvailableThreads(out int freeWorkerThreads, out int freeIoThreads);
        #endif

        TcpClient client = await _server.AcceptTcpClientAsync();
        if (client == null)
        {
            return;
        }

        _ = Task.Run(() => readHandleClient(client));
    }


    private async Task readHandleClient(TcpClient client)
    {
        _connect = true;
        _acceptTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _stream = client.GetStream();

        while (true)
        {
            _connect = true;
            // read
            byte[] recBuffer = new byte[MAX_TCP_MESSAGE];
            int recLength = 0;
            try
            {
                recLength = _stream.Read(recBuffer, 0, recBuffer.Length);
            }
            catch
            {
                _connect = false;
                break;
            }
            if (recLength > 0)
            {
                DataReceivedNotify?.Invoke(recBuffer, recLength);
            }
        }

        string comment = "client out";
        byte[] bc = new byte[comment.Length];
        for (int i = 0; i < bc.Length; i++)
        {
            bc[i] = (byte)comment[i];
        }
        DataReceivedNotify?.Invoke(bc, comment.Length);
        _acceptTimer.Change(TIME_OUT_ACCEPT, TIME_OUT_ACCEPT);
    }


    private void write(object obj)
    {
        #if DEBUG
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIoThreads);
        ThreadPool.GetAvailableThreads(out int freeWorkerThreads, out int freeIoThreads);
        #endif

        if (!_connect || _stream == null)
        {
            return;
        }
        if (_sendData.Count <= 0)
        {
            _writeTimer.Change(Timeout.Infinite, Timeout.Infinite);
            return;
        }
        int sendCount = Math.Min(MAX_TCP_MESSAGE, _sendData.Count);
        try
        {
            _stream.WriteAsync(_sendData.GetRange(0, sendCount).ToArray(), 0, sendCount);
            _sendData.RemoveRange(0, sendCount);
        }
        catch
        {
            _stream = null;
        }
    }


    private readonly TcpListener _server;
    private NetworkStream _stream;
    private readonly Timer _acceptTimer;
    private readonly Timer _writeTimer;
    private readonly List<byte> _sendData = [];
    private bool _enable;
    private bool _connect = false;
}
