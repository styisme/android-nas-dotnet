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
        // 动态刷新标题
        Title = LocalizationManager.Get("title_home");
        
#if ANDROID
        var running = ServerState.IsRunning;
        Subtitle.Text = running ? LocalizationManager.Get("running") : LocalizationManager.Get("stopped");
        ToggleBtn.Text = running ? LocalizationManager.Get("stop") : LocalizationManager.Get("start");
        ServiceStatusLabel.Text = LocalizationManager.Get("service_status");
#endif
    }
    // 其余代码不变...
}
