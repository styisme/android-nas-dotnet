using System.Net;
using System.Net.Sockets;
using Microsoft.Maui.Controls;

namespace AndroidNas.Client.Pages;

public partial class DiscoveryPage : ContentPage
{
    public DiscoveryPage() { InitializeComponent(); }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IpEntry.Text)) return;
        
        // 简单测试连接
        var url = $"http://{IpEntry.Text}:{PortEntry.Text}/api/status";
        try
        {
            using var client = new HttpClient();
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{UserEntry.Text}:{PassEntry.Text}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            
            Loading.IsRunning = true; Loading.IsVisible = true;
            var res = await client.GetStringAsync(url);
            Loading.IsRunning = false; Loading.IsVisible = false;

            if (res.Contains("globalMode"))
            {
                CurrentServer.Init(IpEntry.Text, int.Parse(PortEntry.Text), UserEntry.Text, PassEntry.Text);
                await Shell.Current.GoToAsync("files");
            }
        }
        catch
        {
            Loading.IsRunning = false; Loading.IsVisible = false;
            await DisplayAlertAsync("错误", "无法连接到该 NAS 服务端", "好");
        }
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        Loading.IsRunning = true; Loading.IsVisible = true;
        DeviceList.ItemsSource = null;
        var found = new List<DeviceItem>();

        // 简易局域网扫描 (扫描常见的 192.168.x.x / 10.x.x.x)
        var localIp = GetLocalIp();
        if (localIp == "127.0.0.1") return;
        
        var parts = localIp.Split('.');
        if (parts.Length < 3) return;
        var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";

        var tasks = new List<Task>();
        for (int i = 1; i < 255; i++)
        {
            var ip = prefix + i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(200) };
                    var res = await client.GetAsync($"http://{ip}:8080/api/status");
                    if (res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        lock (found) { found.Add(new DeviceItem { Ip = ip }); }
                    }
                }
                catch { }
            }));
        }

        await Task.WhenAll(tasks);
        
        Loading.IsRunning = false; Loading.IsVisible = false;
        DeviceList.ItemsSource = found;
        if (found.Count == 0) await DisplayAlertAsync("扫描完成", "未发现局域网 NAS", "好");
    }

    private async void OnDeviceTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is DeviceItem device)
        {
            IpEntry.Text = device.Ip;
            PortEntry.Text = "8080";
        }
    }

    private string GetLocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint ep) return ep.Address.ToString();
        }
        catch { }
        return "127.0.0.1";
    }
    
    public class DeviceItem { public string Ip { get; set; } = ""; }
}
