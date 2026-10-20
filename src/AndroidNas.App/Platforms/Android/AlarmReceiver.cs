using Android.App;
using Android.Content;
using Android.OS;

namespace AndroidNas.App.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class AlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        if (HttpServerService.IsRunning) return;

        try
        {
            var svc = new Intent(context, typeof(HttpServerService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                context.StartForegroundService(svc);
            else
                context.StartService(svc);
        }
        catch { }
    }
}
