using EasyFastTcpSerialWrapper;
using System;

namespace WrapTester;

public delegate void ClientReceivedMessages(byte[][] messages);

public class Client
{
    public Client()
    {
        _wrapper = new ByteWrapper();
        _tcpClient = new FastEasyTcpClient("localhost", 22022);
        _tcpClient.DataReceived += routeReceived;
    }


    public void ConnectToServer()
    {
        _tcpClient.Connect();
    }


    public void Disconnect()
    {
        _tcpClient.Disconnect();
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
        ProcessReceivedMessages();
    }


    public event ClientReceivedMessages MessageReceived;


    public void ProcessReceivedMessages()
    {
        byte[][] messages = _wrapper.GetReceivedMessages();
        // process received messages
        MessageReceived?.Invoke(messages);
    }


    private readonly FastEasyTcpClient _tcpClient;
    private readonly ByteWrapper _wrapper;
}
