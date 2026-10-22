using AndroidNas.Core;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;
using System.Threading.Tasks;

namespace AndroidNas.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        NasLog.Init(); // 自动使用 FileSystem.AppDataDirectory/Logs
        
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            NasLog.Crash(e.Exception);
            e.SetObserved();
        };

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}
