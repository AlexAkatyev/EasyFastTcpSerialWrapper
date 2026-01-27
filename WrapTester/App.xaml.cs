using System.Windows;

namespace WrapTester;

public partial class App : Application
{
    private void applicationStartup(object sender, StartupEventArgs e)
    {
        _server = new Server();
        _client= new Client();
        createMainWindow();
    }


    private void createMainWindow()
    {
        MainWindow mw = new MainWindow();
        Application.Current.MainWindow = mw;

        SCViewModel viewModel = new SCViewModel();
        viewModel.SetServer(_server);
        viewModel.SetClient(_client);
        mw.DataContext = viewModel;

        mw.Show();
        mw.WindowState = WindowState.Normal;
    }

    private Server _server;
    private Client _client;
}
