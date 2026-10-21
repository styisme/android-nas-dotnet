#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif
using AndroidNas.Core;
using Microsoft.Maui.Controls;

namespace AndroidNas.App.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        LocalizationManager.LanguageChanged += OnLanguageChanged;
#if ANDROID
        ServerState.Changed += () => MainThread.BeginInvokeOnMainThread(Refresh);
#endif
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(Refresh);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
        Title = LocalizationManager.Get("title_home");
        
#if ANDROID
        var running = ServerState.IsRunning;
        Subtitle.Text = running ? LocalizationManager.Get("running") : LocalizationManager.Get("stopped");
        ToggleBtn.Text = running ? LocalizationManager.Get("stop") : LocalizationManager.Get("start");
        ServiceStatusLabel.Text = LocalizationManager.Get("service_status");
        AddressLabel.Text = "http://" + NetworkHelper.GetLocalIp() + ":" + AppSettings.WebPort;
        PortsLabel.Text = "HTTP:" + AppSettings.WebPort + "   FTP:" + AppSettings.FtpPort + "   DAV:" + AppSettings.WebDavPort;

        var root = AdvancedKeepAliveManager.HasRoot();
        var doze = AdvancedKeepAliveManager.IsDozeWhitelisted(Microsoft.Maui.ApplicationModel.Platform.AppContext);
        KeepAliveStatus.Text = "Root: " + (root ? "✅" : "❌") + "   Doze: " + (doze ? "✅" : "❌") + "   静音保活: " + (AppSettings.SilentKeepAlive ? "✅" : "❌");
#endif
    }

    private async void OnToggleService(object sender, EventArgs e)
    {
#if ANDROID
        await ServerState.ToggleAsync();
        await Task.Delay(400);
        Refresh();
#else
        await Task.CompletedTask;
#endif
    }

    // 补全所有的导航事件
    private async void OnFiles(object sender, TappedEventArgs e) => await Navigation.PushAsync(new FilesPage());
    private async void OnSettings(object sender, TappedEventArgs e) => await Navigation.PushAsync(new SettingsPage());
    private async void OnLogs(object sender, TappedEventArgs e) => await Navigation.PushAsync(new LogsPage());
    private async void OnKeepAlive(object sender, TappedEventArgs e) => await Navigation.PushAsync(new KeepAlivePage());
    private async void OnShell(object sender, TappedEventArgs e) => await Navigation.PushAsync(new ShellPage());
    private async void OnClipboard(object sender, TappedEventArgs e) => await Navigation.PushAsync(new ClipboardPage());
    private async void OnTunnel(object sender, TappedEventArgs e) => await Navigation.PushAsync(new TunnelPage());
    private async void OnWeb(object sender, TappedEventArgs e) => await Navigation.PushAsync(new WebViewPage());
    private async void OnAbout(object sender, TappedEventArgs e) => await Navigation.PushAsync(new AboutPage());
}
