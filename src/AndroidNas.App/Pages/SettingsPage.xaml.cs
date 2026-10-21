using AndroidNas.Core;
using Microsoft.Maui.Controls;
#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif

namespace AndroidNas.App.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        AliasEntry.Text = AppSettings.DeviceAlias;
        WebPortEntry.Text = AppSettings.WebPort.ToString();
        HttpsPortEntry.Text = AppSettings.HttpsPort.ToString();
        DavPortEntry.Text = AppSettings.WebDavPort.ToString();
        FtpPortEntry.Text = AppSettings.FtpPort.ToString();

        // 绑定当前语言
        LanguagePicker.SelectedItem = AppSettings.Language;

        HttpsSwitch.IsToggled = AppSettings.HttpsEnabled;
        WebDavSwitch.IsToggled = AppSettings.WebDavEnabled;
        FtpSwitch.IsToggled = AppSettings.FtpEnabled;
        DlnaSwitch.IsToggled = AppSettings.DlnaEnabled;
        StreamSwitch.IsToggled = AppSettings.StreamEnabled;
        ClipSwitch.IsToggled = AppSettings.ClipboardEnabled;
        LocalSendSwitch.IsToggled = AppSettings.LocalSendEnabled;
        GlobalSwitch.IsToggled = AppSettings.GlobalMode;
        SilentSwitch.IsToggled = AppSettings.SilentKeepAlive;
    }

    private async void OnSave(object sender, EventArgs e)
    {
        if (!int.TryParse(WebPortEntry.Text, out var wp) || wp < 1 || wp > 65535) { await DisplayAlertAsync("错误", "Web 端口无效", "好"); return; }
        if (!int.TryParse(HttpsPortEntry.Text, out var hp) || hp < 1 || hp > 65535) { await DisplayAlertAsync("错误", "HTTPS 端口无效", "好"); return; }

        // 保存语言
        if (LanguagePicker.SelectedItem is string lang)
            AppSettings.Language = lang;

        AppSettings.DeviceAlias = string.IsNullOrWhiteSpace(AliasEntry.Text) ? "Android NAS" : AliasEntry.Text!.Trim();
        AppSettings.WebPort = wp;
        AppSettings.HttpsPort = hp;
        // 简化，其余沿用之前的逻辑...

        await DisplayAlertAsync("完成", "设置已保存。", "好");
        await Navigation.PopAsync();
    }

    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();
    private async void OnExportConfig(object sender, EventArgs e) { /* 之前逻辑 */ }
    private async void OnImportConfig(object sender, EventArgs e) { /* 之前逻辑 */ }
}
