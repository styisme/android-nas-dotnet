using System;
using System.IO;
using AndroidNas.Core;

namespace AndroidNas.DesktopClient;

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

        Console.WriteLine("NAS Client initialized.");
        Console.WriteLine($"Logs are saved to: {logDir}");
        Console.WriteLine("Type 'l' to view logs, 'q' to quit.");
        
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
    }
}
