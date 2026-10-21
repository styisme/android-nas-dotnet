namespace AndroidNas.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("files", typeof(Pages.FilesPage));
    }
}
