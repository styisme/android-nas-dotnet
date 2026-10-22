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
        // 客户端启动即捕获全局异常
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            NasLog.Crash(e.Exception);
            e.SetObserved();
        };

        // 客户端日志统一放到沙盒的 Logs 目录
        var logDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "Logs");
        NasLog.Init(logDir);

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}
