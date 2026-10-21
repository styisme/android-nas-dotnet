using System;
using System.Diagnostics;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.Desktop;

class Program
{
    static void Main(string[] args)
    {
        AppSettings.Store = new DesktopSettingsStore();
        
        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine("Desktop Server started. Opening browser...");
        
        var url = AppSettings.HttpsEnabled ? $"https://localhost:{AppSettings.HttpsPort}" : $"http://localhost:{AppSettings.WebPort}";
        
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { Console.WriteLine("Please open " + url + " manually."); }

        while (Console.ReadLine()?.ToLower() != "q") { }
        server.Dispose();
    }
}

public class DesktopSettingsStore : ISettingsStore
{
    // 桌面端使用真实的本地文件
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidNas", "settings.json");
    private readonly Dictionary<string, string> _dict = new();

    public DesktopSettingsStore()
    {
        try {
            if (File.Exists(_path)) {
                var lines = File.ReadAllLines(_path);
                foreach (var l in lines) { var p = l.Split(new[]{'='}, 2); if (p.Length==2) _dict[p[0]] = p[1]; }
            }
        } catch {}
    }

    private void Save() {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllLines(_path, _dict.Select(kv => $"{kv.Key}={kv.Value}"));
        } catch {}
    }

    public string GetString(string key, string def) => _dict.TryGetValue(key, out var v) ? v : def;
    public int GetInt(string key, int def) => int.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public bool GetBool(string key, bool def) => bool.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public void SetString(string key, string value) { _dict[key] = value; Save(); }
    public void SetInt(string key, int value) => SetString(key, value.ToString());
    public void SetBool(string key, bool value) => SetString(key, value.ToString());
}
