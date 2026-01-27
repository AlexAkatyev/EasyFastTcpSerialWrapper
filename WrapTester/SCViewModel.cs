using System.Runtime.InteropServices;
using System.Windows.Input;

namespace WrapTester;

public class SCViewModel
{
    public void SetServer(Server server)
    {
        _server = server;
    }


    public void SetClient(Client client)
    {
        _client = client;
    }



    private bool _onServer = false;
    public bool OnServer
    {
        get
        {
            return _onServer;
        }
        set
        {
            _onServer = value;
        }
    }


    private bool _onClient = false;
    public bool OnClient
    {
        get
        {
            return _onClient;
        }
        set
        {
            _onClient = value;
        }
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
        }
    }


    private Server _server;
    private Client _client;
}
