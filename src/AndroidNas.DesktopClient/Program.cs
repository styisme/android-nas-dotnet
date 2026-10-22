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

    static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };
        NasLog.Init(System.IO.Path.Combine(AppContext.BaseDirectory, "Logs"));

        // 省略后续交互逻辑...
        Console.WriteLine("NAS Client initialized. Logs at " + AppContext.BaseDirectory + "Logs");
    }
}
