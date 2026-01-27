----------------------------------------------------
Tcp Server example:
----------------------------------------------------

using EasyFastTcpSerialWrapper;

...

    private readonly TcpServer _tcpServer;
    private readonly ByteWrapper _wrapper;

...

    _wrapper = new ByteWrapper();
    _tcpServer = new TcpServer(IPAddress.Parse(address), port);
    _tcpServer.DataReceivedNotify += receivedData;

...

    public void PostMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
    }

...

    public void SendMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
        TcpServerSend();
    }

...

    public void TcpServerSend()
    {
        byte[] toSend = _wrapper.GetDataToSend();
        _tcpServer.Send(toSend, 0, toSend.Length);
    }

...

    private void receivedData(byte[] data, int length)
    {
        byte[] input = new byte[length];
        Array.Copy(data, input, length);
        _wrapper.ReceiveData(input);
        byte[][] receivedMessages = _wrapper.GetReceivedMessages();
        // process received messages
    }



----------------------------------------------------
Tcp Client example:
----------------------------------------------------

using EasyFastTcpSerialWrapper;

...

    private readonly FastEasyTcpClient _tcpClient;
    private readonly ByteWrapper _wrapper;

...

    _wrapper = new ByteWrapper();
    _tcpClient = new FastEasyTcpClient(HOST_ADDRESS, HOST_PORT);
    _tcpClient.DataReceived += routeReceived;

...

    public void PostMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
    }

...

    public void SendMessage(byte[] message)
    {
        _wrapper.SendMessage(message);
        TcpClientSend();
    }

...

    public void TcpClientSend()
    {
        byte[] toSend = _wrapper.GetDataToSend();
        _tcpClient.Send(toSend, 0, toSend.Length);
    }

...

    private void routeReceived(byte[] data, int count)
    {
        byte[] input = new byte[count];
        Array.Copy(data, input, count);
        _wrapper.ReceiveData(input);
    }

...

    public void ProcessReceivedMessages()
    {
        byte[][] messages = _wrapper.GetReceivedMessages();
        // process received messages
    }


