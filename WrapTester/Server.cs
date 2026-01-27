using EasyFastTcpSerialWrapper;
using System;
using System.Net;

namespace WrapTester;

public class Server
{
    public Server()
    {
        _wrapper = new ByteWrapper();
        _tcpServer = new TcpServer(IPAddress.Parse("127.0.0.1"), 22022);
        _tcpServer.DataReceivedNotify += receivedData;
    }


    public void PostMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
    }


    public void SendMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
        TcpServerSend();
    }


    public void TcpServerSend()
    {
        byte[] toSend = _wrapper.GetDataToSend();
        _tcpServer.Send(toSend, 0, toSend.Length);
    }


    private void receivedData(byte[] data, int length)
    {
        byte[] input = new byte[length];
        Array.Copy(data, input, length);
        _wrapper.ReceiveData(input);
        byte[][] receivedMessages = _wrapper.GetReceivedMessages();
        // process received messages
    }

    private readonly TcpServer _tcpServer;
    private readonly ByteWrapper _wrapper;
}
