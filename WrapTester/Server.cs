using EasyFastTcpSerialWrapper;
using System;
using System.Net;
using System.Windows;

namespace WrapTester;

public class Server
{
    private const int PORT = 22022;

    public Server()
    {
        _wrapper = new ByteWrapper();
        _tcpServer = new TcpServer(IPAddress.Parse("127.0.0.1"), PORT);
        _tcpServer.DataReceivedNotify += receivedData;
        if (!_tcpServer.Start())
        {
            MessageBox.Show
             (
                "Do not hold the port "
                    + PORT.ToString()
                , "MyProgram"
            );
        }
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
