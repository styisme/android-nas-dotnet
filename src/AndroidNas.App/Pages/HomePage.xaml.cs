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
#if ANDROID
        ServerState.Changed += () => 
        {
            try { MainThread.BeginInvokeOnMainThread(Refresh); } catch { }
        };
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
        try
        {
            Title = LocalizationManager.Get("title_home");
#if ANDROID
            var running = ServerState.IsRunning;
            if (ServiceStatusLabel != null) ServiceStatusLabel.Text = running ? LocalizationManager.Get("running") : LocalizationManager.Get("stopped");
            if (ToggleBtn != null) ToggleBtn.Text = running ? LocalizationManager.Get("stop") : LocalizationManager.Get("start");
            if (AddressLabel != null) AddressLabel.Text = "http://" + NetworkHelper.GetLocalIp() + ":" + AppSettings.WebPort;
            if (PortsLabel != null) PortsLabel.Text = "HTTP:" + AppSettings.WebPort + "   FTP:" + AppSettings.FtpPort + "   DAV:" + AppSettings.WebDavPort;
            if (Subtitle != null) Subtitle.Text = running ? "服务运行中" : "服务已停止";

            var root = AdvancedKeepAliveManager.HasRoot();
            var doze = AdvancedKeepAliveManager.IsDozeWhitelisted(Microsoft.Maui.ApplicationModel.Platform.AppContext);
            if (KeepAliveStatus != null) 
                KeepAliveStatus.Text = "Root: " + (root ? "✅" : "❌") + "   Doze: " + (doze ? "✅" : "❌");
#endif
        }
        catch (Exception ex)
        {
            NasLog.Crash(ex);
        }
    }

    private async void OnToggleService(object sender, EventArgs e)
    {
        try
        {
#if ANDROID
            await ServerState.ToggleAsync();
            await Task.Delay(400);
            Refresh();
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            NasLog.Crash(ex);
            await DisplayAlertAsync("错误", ex.Message, "好");
        }
    }

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
