using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace EasyFastTcpSerialWrapper;

public delegate void RouteDataReceived(byte[] data, int count);

public class FastEasyTcpClient
{
    private const int ACCEPT_TIME_OUT = 100;
    private const int RECEIVE_TIME_OUT = 100;
    private const int WRITE_TIME_OUT = 100;
    private const int MAX_TCP_MESSAGE = 1500;

    public FastEasyTcpClient(string hostName, int port)
    {
        _hostName = hostName;
        _port = port;
        _acceptTimer = new Timer
        (
            new TimerCallback(acceptToServer)
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


    public void Connect()
    {
        _enable = true;
        _acceptTimer.Change(0, ACCEPT_TIME_OUT);
    }


    public void Disconnect()
    {
        _stream?.Dispose();
        _stream = null;
        _client?.Close();
        _client = null;
        _readTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _acceptTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _enable = false;
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
            _writeTimer.Change(0, WRITE_TIME_OUT);
        }
    }


    public event RouteDataReceived DataReceived;


    private void acceptToServer(object obj)
    {
        if (_client == null)
        {
            _stream?.Dispose();
            _stream = null;
            _client = new TcpClient();
        }
        if (!_client.Connected)
        {
            try
            {
                _client.Connect(_hostName, _port);
                _stream = _client.GetStream();
            }
            catch
            {
                _stream?.Dispose();
                _stream = null;
                _client?.Close();
                _client = null;
            }
        }
        if (_stream != null)
        {
            _readTimer.Change(RECEIVE_TIME_OUT, RECEIVE_TIME_OUT);
            _acceptTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }


    private void read(object obj)
    {
        if (_stream == null)
        {
            if (_enable)
            {
                _acceptTimer.Change(ACCEPT_TIME_OUT, ACCEPT_TIME_OUT);
            }
            return;
        }
        byte[] buffer = new byte[MAX_TCP_MESSAGE];
        int recLength = 0;
        try
        {
            recLength = _stream.Read(buffer, 0, buffer.Length);
        }
        catch
        {
            _stream?.Dispose();
            _stream = null;
            _client?.Close();
            _client = null;
            _readTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (_enable)
            {
                _acceptTimer.Change(ACCEPT_TIME_OUT, ACCEPT_TIME_OUT);
            }
        }
        if (recLength > 0)
        {
            DataReceived?.Invoke(buffer, recLength);
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


    private TcpClient _client;
    private NetworkStream _stream;
    private string _hostName;
    private int _port;
    private readonly Timer _acceptTimer;
    private readonly Timer _readTimer;
    private readonly Timer _writeTimer;
    private bool _enable;
    private readonly List<byte> _sendData = [];
}
