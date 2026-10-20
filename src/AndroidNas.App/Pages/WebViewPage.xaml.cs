#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif
using AndroidNas.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace AndroidNas.App.Pages;

public partial class WebViewPage : ContentPage
{
    private string _url = "";

    public WebViewPage()
    {
        InitializeComponent();
#if ANDROID
        _url = "http://127.0.0.1:" + AppSettings.WebPort + "/";
#else
        _url = "http://127.0.0.1:8080/";
#endif
        UrlLabel.Text = _url;
        Browser.Source = _url;
    }

    private void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        UrlLabel.Text = e.Url;
    }

    private void OnReload(object sender, EventArgs e)
    {
        Browser.Source = _url;
    }

    private async void OnExternal(object sender, EventArgs e)
    {
        try { await Launcher.Default.OpenAsync(_url); }
        catch { }
    }
}
