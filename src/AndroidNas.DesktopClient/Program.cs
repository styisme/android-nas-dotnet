using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AndroidNas.Core;

namespace AndroidNas.DesktopClient;

class Program
{
    static HttpClient _http = new HttpClient();
    static string _currentPath = "/";
    static string _lang = "zh-CN";

    static async Task Main(string[] args)
    {
        // 语言选择
        Console.Write("Choose language (1. 中文  2. English) [Default 1]: ");
        var choice = Console.ReadLine();
        _lang = (choice == "2") ? "en-US" : "zh-CN";
        LocalizationManager.SetLanguage(_lang);

        Console.WriteLine(LocalizationManager.Get("cli_welcome"));
        
        Console.Write(LocalizationManager.Get("cli_ip"));
        var ip = Console.ReadLine() ?? "127.0.0.1";
        
        Console.Write(LocalizationManager.Get("cli_port"));
        var port = Console.ReadLine();
        if (string.IsNullOrEmpty(port)) port = "8080";

        Console.Write(LocalizationManager.Get("cli_user"));
        var user = Console.ReadLine();
        if (string.IsNullOrEmpty(user)) user = "admin";

        Console.Write(LocalizationManager.Get("cli_pass"));
        var pass = Console.ReadLine();
        if (string.IsNullOrEmpty(pass)) pass = "admin";

        _http.BaseAddress = new Uri($"http://{ip}:{port}");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        try
        {
            var res = await _http.GetAsync("/api/status");
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine(LocalizationManager.Get("cli_conn_fail"));
                return;
            }
            Console.WriteLine(LocalizationManager.Get("cli_conn_success"));
        }
        catch
        {
            Console.WriteLine(LocalizationManager.Get("cli_conn_fail"));
            return;
        }

        while (true)
        {
            Console.Write($"NAS:{_currentPath}$ ");
            var cmd = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(cmd)) continue;
            if (cmd == "exit" || cmd == "quit") break;

            var parts = cmd.Split(' ', 2);
            var action = parts[0].ToLower();
            var arg = parts.Length > 1 ? parts[1] : "";

            switch (action)
            {
                case "ls":
                    await ListFiles();
                    break;
                case "cd":
                    await ChangeDir(arg);
                    break;
                case "get":
                    await DownloadFile(arg);
                    break;
                case "help":
                    Console.WriteLine(LocalizationManager.Get("cli_help"));
                    break;
                default:
                    Console.WriteLine(LocalizationManager.Get("cli_unknown"));
                    break;
            }
        }
    }

    static async Task ListFiles()
    {
        var res = await _http.GetStringAsync($"/api/list?path={Uri.EscapeDataString(_currentPath)}");
        var doc = System.Text.Json.JsonDocument.Parse(res);
        foreach (var el in doc.RootElement.GetProperty("files").EnumerateArray())
        {
            var isDir = el.GetProperty("dir").GetBoolean();
            var name = el.GetProperty("name").GetString();
            Console.WriteLine((isDir ? "📁 " : "📄 ") + name);
        }
    }

    static async Task ChangeDir(string dir)
    {
        if (dir == "..")
        {
            if (_currentPath != "/")
            {
                var parts = _currentPath.TrimEnd('/').Split('/');
                _currentPath = string.Join("/", parts.Take(parts.Length - 1));
                if (string.IsNullOrEmpty(_currentPath)) _currentPath = "/";
            }
            return;
        }
        
        _currentPath = _currentPath.TrimEnd('/') + "/" + dir;
        try { await ListFiles(); } 
        catch { 
            Console.WriteLine(LocalizationManager.Get("cli_dir_error"));
            _currentPath = "/";
        }
    }

    static async Task DownloadFile(string filename)
    {
        var path = _currentPath.TrimEnd('/') + "/" + filename;
        var bytes = await _http.GetByteArrayAsync($"/api/download?path={Uri.EscapeDataString(path)}");
        await System.IO.File.WriteAllBytesAsync(filename, bytes);
        Console.WriteLine(string.Format(LocalizationManager.Get("cli_downloaded"), filename));
    }
}
