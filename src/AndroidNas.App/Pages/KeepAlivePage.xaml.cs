using AndroidNas.Core;
using Microsoft.Maui.Controls;

namespace AndroidNas.App.Pages;

public partial class KeepAlivePage : ContentPage
{
    public KeepAlivePage() { InitializeComponent(); }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

#if ANDROID
    private void Refresh()
    {
        var ctx = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        var root = Platforms.Android.AdvancedKeepAliveManager.HasRoot();
        var doze = Platforms.Android.AdvancedKeepAliveManager.IsDozeWhitelisted(ctx);
        var running = Platforms.Android.ServerState.IsRunning;

        RootLabel.Text = "Root: " + (root ? "✅ 已获取" : "❌ 未获取");
        DozeLabel.Text = "Doze 白名单: " + (doze ? "✅ 已加入" : "❌ 未加入");
        ServiceLabel.Text = "服务: " + (running ? "✅ 运行中" : "❌ 已停止");

        var logs = NasLog.Snapshot();
        LogLabel.Text = logs.Length == 0 ? "(暂无)" : string.Join("\n", logs.Take(8));
    }

    private async void OnApplyExtreme(object sender, EventArgs e)
    {
        var ctx = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        if (!Platforms.Android.AdvancedKeepAliveManager.HasRoot())
        {
            await DisplayAlert("需要 Root", "当前设备未获取 Root 权限。", "知道了");
            return;
        }
        await Task.Run(() => Platforms.Android.AdvancedKeepAliveManager.ApplyExtremeWhitelist(ctx));
        Refresh();
        await DisplayAlert("完成", "极致保活注入完成。", "好");
    }

    private async void OnGenerateMagisk(object sender, EventArgs e)
    {
        var ctx = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        if (!Platforms.Android.AdvancedKeepAliveManager.HasRoot())
        {
            await DisplayAlert("需要 Root", "当前设备未获取 Root 权限。", "知道了");
            return;
        }
        var ok = await Task.Run(() => Platforms.Android.AdvancedKeepAliveManager.GenerateMagiskModule(ctx));
        await DisplayAlert("Magisk 模块", ok ? "已生成，重启手机后生效。" : "生成失败。", "好");
    }

    private async void OnRestoreServices(object sender, EventArgs e)
    {
        await Task.Run(() => Platforms.Android.AdvancedKeepAliveManager.RestoreSystemServices());
        await DisplayAlert("完成", "厂商系统服务已还原。", "好");
    }
#else
    private void Refresh() { }
    private void OnApplyExtreme(object sender, EventArgs e) { }
    private void OnGenerateMagisk(object sender, EventArgs e) { }
    private void OnRestoreServices(object sender, EventArgs e) { }
#endif
}
