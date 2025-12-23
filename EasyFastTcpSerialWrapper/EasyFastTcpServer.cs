using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EasyFastTcpSerialWrapper;


public class TcpServer
{
    public delegate void TcpServerReceiveHandler(byte[] data, int length);
    private const int TIME_OUT_WAIT = 0;
    private const int TIME_OUT_ACCEPT = 100;
    private const int TIME_OUT_READ = 100;
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
        _readTimer = new Timer
        (
            new TimerCallback(read)
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
            _readTimer.Change(TIME_OUT_READ, TIME_OUT_READ);
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
        _readTimer.Change(Timeout.Infinite, Timeout.Infinite);
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


    private void acceptClient(object obj)
    {
        if (_client == null)
        {
            _stream = null;
            _client = _server.AcceptTcpClient();
        }
        if (_client == null)
        {
            return;
        }
        if (_stream == null)
        {
            _stream = _client.GetStream();
        }
    }


    private void read(object obj)
    {
        if (_stream == null)
        {
            return;
        }
        byte[] recBuffer = new byte[MAX_TCP_MESSAGE];
        int recLength = 0;
        try
        {
            recLength = _stream.Read(recBuffer, 0, recBuffer.Length);
        }
        catch
        {
            _stream = null;
            _client = null;
        }
        if (recLength > 0)
        {
            DataReceivedNotify?.Invoke(recBuffer, recLength);
        }
    }


    private void write(object obj)
    {
        if (_stream == null)
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
            _stream.Write(_sendData.GetRange(0, sendCount).ToArray(), 0, sendCount);
            _sendData.RemoveRange(0, sendCount);
        }
        catch
        {
            _stream = null;
            _client = null;
        }
    }


    private readonly TcpListener _server;
    private TcpClient _client;
    private NetworkStream _stream;
    private readonly Timer _acceptTimer;
    private readonly Timer _readTimer;
    private readonly Timer _writeTimer;
    private readonly List<byte> _sendData = [];
    private bool _enable;
}
