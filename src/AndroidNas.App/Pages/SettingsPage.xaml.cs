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
        if (!int.TryParse(DavPortEntry.Text, out var dp) || dp < 1 || dp > 65535) { await DisplayAlertAsync("错误", "WebDAV 端口无效", "好"); return; }
        if (!int.TryParse(FtpPortEntry.Text, out var fp) || fp < 1 || fp > 65535) { await DisplayAlertAsync("错误", "FTP 端口无效", "好"); return; }

        AppSettings.DeviceAlias = string.IsNullOrWhiteSpace(AliasEntry.Text) ? "Android NAS" : AliasEntry.Text!.Trim();
        AppSettings.WebPort = wp;
        AppSettings.HttpsPort = hp;
        AppSettings.WebDavPort = dp;
        AppSettings.FtpPort = fp;

        AppSettings.HttpsEnabled = HttpsSwitch.IsToggled;
        AppSettings.WebDavEnabled = WebDavSwitch.IsToggled;
        AppSettings.FtpEnabled = FtpSwitch.IsToggled;
        AppSettings.DlnaEnabled = DlnaSwitch.IsToggled;
        AppSettings.StreamEnabled = StreamSwitch.IsToggled;
        AppSettings.ClipboardEnabled = ClipSwitch.IsToggled;
        AppSettings.LocalSendEnabled = LocalSendSwitch.IsToggled;

        AppSettings.GlobalMode = GlobalSwitch.IsToggled;
        AppSettings.SilentKeepAlive = SilentSwitch.IsToggled;

#if ANDROID
        ServerState.Stop();
        await Task.Delay(800);
        ServerState.Start();
        await Task.Delay(600);
#endif

        await DisplayAlertAsync("完成", "设置已保存，服务已重启。", "好");
        await Navigation.PopAsync();
    }

    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnExportConfig(object sender, EventArgs e)
    {
        var json = AppSettings.ExportToJson();
        var path = Path.Combine(FileSystem.AppDataDirectory, "nas_config_backup.json");
        await File.WriteAllTextAsync(path, json);
        await Share.RequestAsync(new ShareFileRequest { Title = "导出配置", File = new ShareFile(path) });
    }

    private async void OnImportConfig(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "选择配置文件", FileTypes = FilePickerFileType.Json });
            if (result == null) return;
            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            AppSettings.ImportFromJson(json);
            Load();
            await DisplayAlertAsync("成功", "配置已导入，请保存并重启服务生效", "好");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", ex.Message, "好");
        }
    }
}
