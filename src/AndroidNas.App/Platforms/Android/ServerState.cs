using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace AndroidNas.App.Platforms.Android;

public static class ServerState
{
    public static bool IsRunning => HttpServerService.IsRunning;
    public static string LastError => HttpServerService.LastError;

    public static event Action? Changed
    {
        add => HttpServerService.StateChanged += value;
        remove => HttpServerService.StateChanged -= value;
    }

    public static void Start()
    {
        var ctx = Platform.AppContext;
        var svc = new Intent(ctx, typeof(HttpServerService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            ctx.StartForegroundService(svc);
        else
            ctx.StartService(svc);
    }

    public static void Stop()
    {
        var ctx = Platform.AppContext;
        ctx.StopService(new Intent(ctx, typeof(HttpServerService)));
    }

    public static Task ToggleAsync() => Task.Run(() =>
    {
        if (IsRunning) Stop();
        else Start();
    });
}
