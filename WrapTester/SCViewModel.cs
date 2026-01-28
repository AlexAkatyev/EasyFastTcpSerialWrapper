using GalaSoft.MvvmLight.Command;
using System.Collections;
using System.Text;
using System.Windows.Input;

namespace WrapTester;

public class SCViewModel
: ViewModelBase
{
    public void SetServer(Server server)
    {
        _server = server;
        _server.MessageReceived += serverReceived;
    }


    public void SetClient(Client client)
    {
        _client = client;
        _client.MessageReceived += clientReceived;
    }


    private ICommand? _cmdServerPost = null;
    public ICommand CmdServerPost => _cmdServerPost ??= new RelayCommand(serverPost);

    private void serverPost()
    {
        if (ServerInput.Length > 0)
        {
            _server.PostMessage(Encoding.ASCII.GetBytes(ServerInput));
            ServerInput = "";
        }
    }


    private ICommand? _cmdServerSend = null;
    public ICommand CmdServerSend => _cmdServerSend ??= new RelayCommand(serverSend);

    private void serverSend()
    {
        if (ServerInput.Length > 0)
        {
            _server.SendMessage(Encoding.ASCII.GetBytes(ServerInput));
            ServerInput = "";
        }
    }


    private ICommand? _cmdServerSendToClient = null;
    public ICommand CmdServerSendToClient => _cmdServerSendToClient ??= new RelayCommand(serverSendToClient);

    private void serverSendToClient()
    {
        _server.TcpServerSend();
    }


    private ICommand? _cmdOnClient = null;
    public ICommand CmdOnClient => _cmdOnClient ??= new RelayCommand(routeConnect);

    private void routeConnect()
    {
        if (!_onClient)
        {
            _client.ConnectToServer();
        }
        else
        {
            _client.Disconnect();
        }
        _onClient = !_onClient;
    }


    private ICommand? _cmdClientPost = null;
    public ICommand CmdClientPost => _cmdClientPost ??= new RelayCommand(clientPost);

    private void clientPost()
    {
        if (ClientInput.Length > 0)
        {
            _client.PostMessage(Encoding.ASCII.GetBytes(ClientInput));
            ClientInput = "";
        }
    }


    private ICommand? _cmdClientSend = null;
    public ICommand CmdClientSend => _cmdClientSend ??= new RelayCommand(clientSend);

    private void clientSend()
    {
        if (ClientInput.Length > 0)
        {
            _client.SendMessage(Encoding.ASCII.GetBytes(ClientInput));
            ClientInput = "";
        }
    }


    private ICommand? _cmdClientSendToServer = null;
    public ICommand CmdClientSendToServer => _cmdClientSendToServer ??= new RelayCommand(clientSendToServer);

    private void clientSendToServer()
    {
        _client.TcpClientSend();
    }


    private string _serverInput = "";
    public string ServerInput
    {
        get
        {
            return _serverInput;
        }
        set
        {
            _serverInput = value;
            OnPropertyChanged();
        }
    }


    private string _clientInput = "";
    public string ClientInput
    {
        get
        {
            return _clientInput;
        }
        set
        {
            _clientInput = value;
            OnPropertyChanged();
        }
    }


    private string _serverOut = "";
    public string ServerOut
    {
        get
        {
            return _serverOut;
        }
        set
        {
            _serverOut = value;
            OnPropertyChanged();
        }
    }


    private string _clientOut = "";
    public string ClientOut
    {
        get
        {
            return _clientOut;
        }
        set
        {
            _clientOut = value;
            OnPropertyChanged();
        }
    }


    private void clientReceived(byte[][] messages)
    {
        for (int i = 0; i < messages.Length; i++)
        {
            string data = Encoding.ASCII.GetString(messages[i]);
            ClientOut += data + "\n";
        }
    }


    private void serverReceived(byte[][] messages)
    {
        for (int i = 0; i < messages.Length; i++)
        {
            string data = Encoding.ASCII.GetString(messages[i]);
            ServerOut += data + "\n";
        }
    }


    private Server _server;
    private Client _client;
    private bool _onClient = false;
}
