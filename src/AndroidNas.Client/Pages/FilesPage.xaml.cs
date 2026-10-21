using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AndroidNas.Client.Pages;

public partial class FilesPage : ContentPage
{
    private string _currentPath = "/";
    private List<FileItem> _items = new();
    private bool _isSelectMode = false;

    public FilesPage()
    {
        InitializeComponent();
        LoadFiles();
    }

    private async void LoadFiles()
    {
        PathLabel.Text = _currentPath;
        try
        {
            var res = await CurrentServer.Http.GetStringAsync($"/api/list?path={Uri.EscapeDataString(_currentPath)}");
            var doc = System.Text.Json.JsonDocument.Parse(res);
            _items.Clear();
            foreach (var el in doc.RootElement.GetProperty("files").EnumerateArray())
            {
                var isDir = el.GetProperty("dir").GetBoolean();
                _items.Add(new FileItem {
                    Name = el.GetProperty("name").GetString() ?? "",
                    IsDir = isDir,
                    Icon = isDir ? "📁" : "📄",
                    Detail = isDir ? "文件夹" : (el.GetProperty("size").GetInt64() / 1024) + " KB",
                    IsSelectMode = _isSelectMode
                });
            }
            FileList.ItemsSource = null;
            FileList.ItemsSource = _items;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", ex.Message, "好");
        }
    }

    private void OnToggleSelect(object sender, EventArgs e)
    {
        _isSelectMode = !_isSelectMode;
        BatchDelBtn.IsVisible = _isSelectMode;
        CastBtn.IsVisible = _isSelectMode;
        foreach (var item in _items) { item.IsSelectMode = _isSelectMode; item.IsSelected = false; }
        FileList.ItemsSource = null;
        FileList.ItemsSource = _items;
    }

    private void OnCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        // 状态已经由双向绑定更新
    }

    private async void OnUp(object sender, EventArgs e)
    {
        if (_currentPath == "/") return;
        var parts = _currentPath.TrimEnd('/').Split('/');
        _currentPath = string.Join("/", parts.Take(parts.Length - 1));
        if (string.IsNullOrEmpty(_currentPath)) _currentPath = "/";
        LoadFiles();
    }

    private async void OnFileTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FileItem file) return;
        
        if (_isSelectMode)
        {
            file.IsSelected = !file.IsSelected;
            FileList.ItemsSource = null;
            FileList.ItemsSource = _items;
            return;
        }

        if (file.IsDir)
        {
            _currentPath = _currentPath.TrimEnd('/') + "/" + file.Name;
            LoadFiles();
        }
        else
        {
            var url = CurrentServer.Http.BaseAddress + $"api/download?path={Uri.EscapeDataString(_currentPath.TrimEnd('/') + "/" + file.Name)}";
            await Launcher.Default.OpenAsync(url);
        }
    }

    private async void OnBatchDelete(object sender, EventArgs e)
    {
        var selected = _items.Where(i => i.IsSelected).ToList();
        if (!selected.Any()) { await DisplayAlertAsync("提示", "未选择文件", "好"); return; }
        if (!await DisplayAlertAsync("确认", $"确定删除 {selected.Count} 个文件？", "删除", "取消")) return;

        foreach (var f in selected)
        {
            var form = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("path", _currentPath.TrimEnd('/') + "/" + f.Name) });
            await CurrentServer.Http.PostAsync("/api/delete", form);
        }
        LoadFiles();
    }

    private async void OnCast(object sender, EventArgs e)
    {
        var selected = _items.FirstOrDefault(i => i.IsSelected && !i.IsDir);
        if (selected == null) { await DisplayAlertAsync("提示", "请选择一个视频或图片文件", "好"); return; }
        
        var url = CurrentServer.Http.BaseAddress + $"api/download?path={Uri.EscapeDataString(_currentPath.TrimEnd('/') + "/" + selected.Name)}";
        
        await Task.Run(() =>
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            var msg = Encoding.UTF8.GetBytes("PLAY:" + url);
            udp.Send(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, 8090));
        });
        await DisplayAlertAsync("投屏", "已向局域网发送投屏指令", "好");
    }

    private async void OnUpload(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync();
        if (result == null) return;
        try
        {
            using var stream = await result.OpenReadAsync();
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(_currentPath), "path");
            content.Add(new StreamContent(stream), "file", result.FileName);
            await CurrentServer.Http.PostAsync("/api/upload", content);
            LoadFiles();
        }
        catch (Exception ex) { await DisplayAlertAsync("错误", ex.Message, "好"); }
    }

    public class FileItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public bool IsDir { get; set; }
        public string Icon { get; set; } = "";
        public string Detail { get; set; } = "";
        
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); } }
        
        private bool _isSelectMode;
        public bool IsSelectMode { get => _isSelectMode; set { _isSelectMode = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelectMode))); } }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}

public static class CurrentServer
{
    public static HttpClient Http { get; private set; } = new HttpClient();
    public static void Init(string ip, int port, string user, string pass)
    {
        Http = new HttpClient { BaseAddress = new Uri($"http://{ip}:{port}") };
        var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{pass}"));
        Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
    }
}
