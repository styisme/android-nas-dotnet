using System;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.Cli;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Android NAS CLI v" + typeof(Program).Assembly.GetName().Version);
        
        // 简易配置初始化
        AppSettings.Store = new CliSettingsStore();
        
        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine($"Web Server started on port {AppSettings.WebPort} (HTTPS: {AppSettings.HttpsEnabled})");
        Console.WriteLine("Press 'q' to quit.");
        
        while (Console.ReadLine()?.ToLower() != "q") { }
        server.Dispose();
    }
}

public class CliSettingsStore : ISettingsStore
{
    private readonly Dictionary<string, string> _dict = new();
    public string GetString(string key, string def) => _dict.TryGetValue(key, out var v) ? v : def;
    public int GetInt(string key, int def) => int.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public bool GetBool(string key, bool def) => bool.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public void SetString(string key, string value) => _dict[key] = value;
    public void SetInt(string key, int value) => SetString(key, value.ToString());
    public void SetBool(string key, bool value) => SetString(key, value.ToString());
}
