using System;
using System.IO;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.DesktopServer;

class Program
{
    static void Main(string[] args)
    {
        string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        NasLog.Init(logDir);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };

        AppSettings.Store = new LocalSettingsStore();
        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine($"Server started on port {AppSettings.WebPort}.");
        Console.WriteLine($"Logs are saved to: {logDir}");
        Console.WriteLine("Press 'q' to quit, or 'l' to view logs.");
        
        while (true)
        {
            var cmd = Console.ReadLine()?.ToLower();
            if (cmd == "q") break;
            if (cmd == "l")
            {
                var logs = NasLog.Snapshot();
                Console.WriteLine(logs.Length == 0 ? "(暂无日志)" : string.Join("\n\n", logs));
            }
        }
        server.Dispose();
    }
}

public class LocalSettingsStore : ISettingsStore
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidNas", "settings.json");
    private readonly Dictionary<string, string> _dict = new();
    public LocalSettingsStore()
    {
        try { if (File.Exists(_path)) { var lines = File.ReadAllLines(_path); foreach (var l in lines) { var p = l.Split(new[]{'='}, 2); if (p.Length==2) _dict[p[0]] = p[1]; } } } catch {}
    }
    private void Save() { try { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllLines(_path, _dict.Select(kv => $"{kv.Key}={kv.Value}")); } catch {} }
    public string GetString(string key, string def) => _dict.TryGetValue(key, out var v) ? v : def;
    public int GetInt(string key, int def) => int.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public bool GetBool(string key, bool def) => bool.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public void SetString(string key, string value) { _dict[key] = value; Save(); }
    public void SetInt(string key, int value) => SetString(key, value.ToString());
    public void SetBool(string key, bool value) => SetString(key, value.ToString());
}
