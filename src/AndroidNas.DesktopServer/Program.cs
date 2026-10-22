using System;
using System.Diagnostics;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.DesktopServer;

class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };

        // PC 服务端日志直接放在程序同级的 Logs 目录
        NasLog.Init(System.IO.Path.Combine(AppContext.BaseDirectory, "Logs"));
        AppSettings.Store = new LocalSettingsStore();
        
        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine($"Server started on port {AppSettings.WebPort}. Logs at {AppContext.BaseDirectory}Logs");
        Console.WriteLine("Press 'q' to quit.");
        while (Console.ReadLine()?.ToLower() != "q") { }
        server.Dispose();
    }
}
// LocalSettingsStore 保持不变...
public class LocalSettingsStore : ISettingsStore
{
    private readonly string _path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidNas", "settings.json");
    private readonly Dictionary<string, string> _dict = new();
    public LocalSettingsStore()
    {
        try { if (System.IO.File.Exists(_path)) { var lines = System.IO.File.ReadAllLines(_path); foreach (var l in lines) { var p = l.Split(new[]{'='}, 2); if (p.Length==2) _dict[p[0]] = p[1]; } } } catch {}
    }
    private void Save() { try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!); System.IO.File.WriteAllLines(_path, _dict.Select(kv => $"{kv.Key}={kv.Value}")); } catch {} }
    public string GetString(string key, string def) => _dict.TryGetValue(key, out var v) ? v : def;
    public int GetInt(string key, int def) => int.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public bool GetBool(string key, bool def) => bool.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public void SetString(string key, string value) { _dict[key] = value; Save(); }
    public void SetInt(string key, int value) => SetString(key, value.ToString());
    public void SetBool(string key, bool value) => SetString(key, value.ToString());
}
