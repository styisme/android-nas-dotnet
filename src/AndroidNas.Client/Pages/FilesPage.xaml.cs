using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AndroidNas.Client.Pages;

public partial class FilesPage : ContentPage
{
    private string _currentPath = "/";

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
            // 简单解析 JSON (为了极简演示，用 System.Text.Json)
            var doc = System.Text.Json.JsonDocument.Parse(res);
            var files = new List<FileItem>();
            foreach (var el in doc.RootElement.GetProperty("files").EnumerateArray())
            {
                var isDir = el.GetProperty("dir").GetBoolean();
                var name = el.GetProperty("name").GetString();
                files.Add(new FileItem {
                    Name = name ?? "",
                    IsDir = isDir,
                    Icon = isDir ? "📁" : "📄",
                    Detail = isDir ? "文件夹" : (el.GetProperty("size").GetInt64() / 1024) + " KB"
                });
            }
            FileList.ItemsSource = files;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", ex.Message, "好");
        }
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
        if (file.IsDir)
        {
            _currentPath = _currentPath.TrimEnd('/') + "/" + file.Name;
            LoadFiles();
        }
        else
        {
            // 下载
            var url = CurrentServer.Http.BaseAddress + $"api/download?path={Uri.EscapeDataString(_currentPath.TrimEnd('/') + "/" + file.Name)}";
            await Launcher.Default.OpenAsync(url);
        }
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
            
            var response = await CurrentServer.Http.PostAsync("/api/upload", content);
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("成功", "上传成功", "好");
                LoadFiles();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", ex.Message, "好");
        }
    }

    public class FileItem
    {
        public string Name { get; set; } = "";
        public bool IsDir { get; set; }
        public string Icon { get; set; } = "";
        public string Detail { get; set; } = "";
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
