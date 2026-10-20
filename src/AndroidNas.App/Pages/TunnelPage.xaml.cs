#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif
using AndroidNas.Core;
using Microsoft.Maui.Controls;
#if ANDROID
using AndroidNas.Server.Tunneling;
#endif

namespace AndroidNas.App.Pages;

public partial class TunnelPage : ContentPage
{
    private IDispatcherTimer? _timer;

    public TunnelPage()
    {
        InitializeComponent();
#if ANDROID
        NodePicker.ItemsSource = FreeTunnelManager.GetNodeNames();
        NodePicker.SelectedIndex = 0;
#endif
        Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => Refresh();
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try { _timer?.Stop(); } catch { }
    }

    private void Refresh()
    {
#if ANDROID
        StatusLabel.Text = FreeTunnelManager.StatusMessage;
        UrlLabel.Text = string.IsNullOrEmpty(FreeTunnelManager.PublicUrl)
            ? ""
            : "公网地址: " + FreeTunnelManager.PublicUrl;
        StartBtn.Text = FreeTunnelManager.IsRunning ? "已连接" : "启动";
#endif
    }

    private async void OnStart(object sender, EventArgs e)
    {
#if ANDROID
        if (FreeTunnelManager.IsRunning) return;
        var idx = NodePicker.SelectedIndex;
        if (idx < 0) idx = 0;
        FreeTunnelManager.Start(AppSettings.WebPort, idx);
        await DisplayAlert("已启动", "正在尝试连接，请稍候…", "好");
#else
        await Task.CompletedTask;
#endif
    }

    private async void OnCopy(object sender, EventArgs e)
    {
#if ANDROID
        if (string.IsNullOrEmpty(FreeTunnelManager.PublicUrl))
        {
            await DisplayAlert("提示", "尚未连接", "好");
            return;
        }
        await Clipboard.Default.SetTextAsync(FreeTunnelManager.PublicUrl);
        await DisplayAlert("已复制", FreeTunnelManager.PublicUrl, "好");
#else
        await Task.CompletedTask;
#endif
    }

    private async void OnCustomSsh(object sender, EventArgs e)
    {
#if ANDROID
        var host = HostEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(host))
        {
            await DisplayAlert("错误", "请填写服务器 IP", "好");
            return;
        }
        if (!int.TryParse(PortEntry.Text, out var port) || port < 1 || port > 65535) port = 22;
        if (!int.TryParse(RemotePortEntry.Text, out var rp) || rp < 1 || rp > 65535) rp = 8080;

        SshTunnelManager.Start(host, port, UserEntry.Text?.Trim() ?? "root",
            PassEntry.Text ?? "", rp, AppSettings.WebPort);

        await DisplayAlert("已提交", "正在后台尝试连接…", "好");
#else
        await Task.CompletedTask;
#endif
    }

    private async void OnStopAll(object sender, EventArgs e)
    {
#if ANDROID
        FreeTunnelManager.Stop();
        SshTunnelManager.Stop();
        await Task.Delay(300);
        Refresh();
        await DisplayAlert("完成", "所有隧道已断开", "好");
#else
        await Task.CompletedTask;
#endif
    }
}
