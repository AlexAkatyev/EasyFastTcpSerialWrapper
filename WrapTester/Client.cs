using EasyFastTcpSerialWrapper;
using System;

namespace WrapTester;

public class Client
{
    public Client()
    {
        _wrapper = new ByteWrapper();
        _tcpClient = new FastEasyTcpClient("localhost", 22022);
        _tcpClient.DataReceived += routeReceived;
    }


    public void PostMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
    }


    public void SendMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
        TcpClientSend();
    }


    public void TcpClientSend()
    {
        byte[] toSend = _wrapper.GetDataToSend();
        _tcpClient.Send(toSend, 0, toSend.Length);
    }


    private void routeReceived(byte[] data, int count)
    {
        byte[] input = new byte[count];
        Array.Copy(data, input, count);
        _wrapper.ReceiveData(input);
    }


    public void ProcessReceivedMessages()
    {
        byte[][] messages = _wrapper.GetReceivedMessages();
        // process received messages
    }


    private readonly FastEasyTcpClient _tcpClient;
    private readonly ByteWrapper _wrapper;
}
