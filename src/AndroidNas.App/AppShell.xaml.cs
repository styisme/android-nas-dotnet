namespace AndroidNas.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("files", typeof(Pages.FilesPage));
        Routing.RegisterRoute("settings", typeof(Pages.SettingsPage));
        Routing.RegisterRoute("logs", typeof(Pages.LogsPage));
        Routing.RegisterRoute("keepalive", typeof(Pages.KeepAlivePage));
        Routing.RegisterRoute("shell", typeof(Pages.ShellPage));
        Routing.RegisterRoute("clipboard", typeof(Pages.ClipboardPage));
        Routing.RegisterRoute("tunnel", typeof(Pages.TunnelPage));
        Routing.RegisterRoute("web", typeof(Pages.WebViewPage));
        Routing.RegisterRoute("about", typeof(Pages.AboutPage));
        Routing.RegisterRoute("preview", typeof(Pages.PreviewPage));
    }
}
