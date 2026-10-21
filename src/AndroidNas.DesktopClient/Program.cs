using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AndroidNas.DesktopClient;

class Program
{
    static HttpClient _http = new HttpClient();
    static string _currentPath = "/";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Android NAS 桌面客户端 ===");
        
        Console.Write("请输入 NAS IP (例如 192.168.1.100): ");
        var ip = Console.ReadLine() ?? "127.0.0.1";
        
        Console.Write("端口 (默认 8080): ");
        var port = Console.ReadLine();
        if (string.IsNullOrEmpty(port)) port = "8080";

        Console.Write("账号 (默认 admin): ");
        var user = Console.ReadLine();
        if (string.IsNullOrEmpty(user)) user = "admin";

        Console.Write("密码 (默认 admin): ");
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
                Console.WriteLine("连接失败，请检查账号密码或 IP。");
                return;
            }
            Console.WriteLine("✅ 连接成功！输入 'help' 查看命令。\n");
        }
        catch
        {
            Console.WriteLine("❌ 无法连接到服务器。");
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
                    Console.WriteLine("ls - 查看目录\ncd <目录> - 切换目录\nget <文件名> - 下载文件\nexit - 退出");
                    break;
                default:
                    Console.WriteLine("未知命令，输入 help 查看帮助");
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
            Console.WriteLine("目录不存在");
            _currentPath = "/";
        }
    }

    static async Task DownloadFile(string filename)
    {
        var path = _currentPath.TrimEnd('/') + "/" + filename;
        var bytes = await _http.GetByteArrayAsync($"/api/download?path={Uri.EscapeDataString(path)}");
        await System.IO.File.WriteAllBytesAsync(filename, bytes);
        Console.WriteLine($"已下载到本地: {filename}");
    }
}
