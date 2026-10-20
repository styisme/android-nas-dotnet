using AndroidNas.Core;
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
        DavPortEntry.Text = AppSettings.WebDavPort.ToString();
        FtpPortEntry.Text = AppSettings.FtpPort.ToString();

        WebDavSwitch.IsToggled = AppSettings.WebDavEnabled;
        FtpSwitch.IsToggled = AppSettings.FtpEnabled;
        DlnaSwitch.IsToggled = AppSettings.DlnaEnabled;
        StreamSwitch.IsToggled = AppSettings.StreamEnabled;
        ClipSwitch.IsToggled = AppSettings.ClipboardEnabled;
        LocalSendSwitch.IsToggled = AppSettings.LocalSendEnabled;

        LowPowerSwitch.IsToggled = AppSettings.LowPowerMode;
        GlobalSwitch.IsToggled = AppSettings.GlobalMode;
        SilentSwitch.IsToggled = AppSettings.SilentKeepAlive;

        PasswordEntry.Text = "";
    }

    private async void OnSave(object sender, EventArgs e)
    {
        if (!int.TryParse(WebPortEntry.Text, out var wp) || wp < 1 || wp > 65535)
        { await DisplayAlert("错误", "Web 端口无效", "好"); return; }
        if (!int.TryParse(DavPortEntry.Text, out var dp) || dp < 1 || dp > 65535)
        { await DisplayAlert("错误", "WebDAV 端口无效", "好"); return; }
        if (!int.TryParse(FtpPortEntry.Text, out var fp) || fp < 1 || fp > 65535)
        { await DisplayAlert("错误", "FTP 端口无效", "好"); return; }

        AppSettings.DeviceAlias = string.IsNullOrWhiteSpace(AliasEntry.Text)
            ? "Android NAS Next" : AliasEntry.Text!.Trim();
        AppSettings.WebPort = wp;
        AppSettings.WebDavPort = dp;
        AppSettings.FtpPort = fp;

        AppSettings.WebDavEnabled = WebDavSwitch.IsToggled;
        AppSettings.FtpEnabled = FtpSwitch.IsToggled;
        AppSettings.DlnaEnabled = DlnaSwitch.IsToggled;
        AppSettings.StreamEnabled = StreamSwitch.IsToggled;
        AppSettings.ClipboardEnabled = ClipSwitch.IsToggled;
        AppSettings.LocalSendEnabled = LocalSendSwitch.IsToggled;

        AppSettings.LowPowerMode = LowPowerSwitch.IsToggled;
        AppSettings.GlobalMode = GlobalSwitch.IsToggled;
        AppSettings.SilentKeepAlive = SilentSwitch.IsToggled;

        var pwd = PasswordEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(pwd))
        {
            if (pwd.Length < 4)
            {
                await DisplayAlert("错误", "密码至少 4 位", "好");
                return;
            }
            AuthService.SetPassword(pwd);
        }

#if ANDROID
        ServerState.Stop();
        await Task.Delay(800);
        ServerState.Start();
        await Task.Delay(600);
#endif

        await DisplayAlert("完成", "设置已保存，服务已重启。", "好");
        await Navigation.PopAsync();
    }

    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();
}
