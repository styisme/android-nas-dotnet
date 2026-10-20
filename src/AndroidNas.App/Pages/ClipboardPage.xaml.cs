#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif
using AndroidNas.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AndroidNas.App.Pages;

public partial class ClipboardPage : ContentPage
{
    private readonly HttpClient _http;

    public ClipboardPage()
    {
        InitializeComponent();
        _http = new HttpClient();
#if ANDROID
        _http.BaseAddress = new Uri("http://127.0.0.1:" + AppSettings.WebPort);
#endif
        var auth = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                AuthService.Username + ":" + AuthService.Password));
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var txt = await _http.GetStringAsync("/api/clipboard");
            ContentEditor.Text = txt;
        }
        catch (Exception e)
        {
            ContentEditor.Text = "读取失败: " + e.Message;
        }
    }

    private async void OnPull(object sender, EventArgs e)
    {
        await LoadAsync();
        await DisplayAlert("完成", "已从服务端拉取", "好");
    }

    private async void OnPush(object sender, EventArgs e)
    {
        try
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>(
                    "content", ContentEditor.Text ?? "")
            });
            var resp = await _http.PostAsync("/api/clipboard", form);
            if (resp.IsSuccessStatusCode)
                await DisplayAlert("完成", "已同步到所有设备", "好");
            else
                await DisplayAlert("失败", "HTTP " + (int)resp.StatusCode, "好");
        }
        catch (Exception e)
        {
            await DisplayAlert("错误", e.Message, "好");
        }
    }
}
