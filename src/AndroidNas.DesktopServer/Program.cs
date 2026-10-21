using System;
using System.Diagnostics;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.DesktopServer;

class Program
{
    static void Main(string[] args)
    {
        var store = new LocalSettingsStore();
        AppSettings.Store = store;
        AppSettings.Language = store.GetString("language", ""); // 读取保存的语言

        // 首次运行让用户选语言
        if (string.IsNullOrEmpty(AppSettings.Language))
        {
            Console.Write("Choose language (1. 中文  2. English) [Default 1]: ");
            var choice = Console.ReadLine();
            AppSettings.Language = (choice == "2") ? "en-US" : "zh-CN";
        }
        LocalizationManager.SetLanguage(AppSettings.Language);

        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine("=====================================");
        Console.WriteLine($"   {LocalizationManager.Get("app_name")} - Desktop Server");
        Console.WriteLine("=====================================");
        Console.WriteLine(string.Format(LocalizationManager.Get("srv_started"), AppSettings.WebPort));
        if (AppSettings.HttpsEnabled) Console.WriteLine(string.Format(LocalizationManager.Get("srv_https"), AppSettings.HttpsPort));
        Console.WriteLine(LocalizationManager.Get("srv_opening"));
        
        var url = AppSettings.HttpsEnabled ? $"https://localhost:{AppSettings.HttpsPort}" : $"http://localhost:{AppSettings.WebPort}";
        
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { Console.WriteLine(string.Format(LocalizationManager.Get("srv_open_manual"), url)); }

        Console.WriteLine(LocalizationManager.Get("srv_stop_hint"));
        while (Console.ReadLine()?.ToLower() != "q") { }
        server.Dispose();
    }
}

public class LocalSettingsStore : ISettingsStore
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidNas", "settings.json");
    private readonly Dictionary<string, string> _dict = new();

    public LocalSettingsStore()
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
