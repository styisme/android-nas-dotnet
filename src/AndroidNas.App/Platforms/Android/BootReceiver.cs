using Android.App;
using Android.Content;
using Android.OS;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        if (intent?.Action != Intent.ActionBootCompleted &&
            intent?.Action != Intent.ActionMyPackageReplaced) return;

        try
        {
            var svc = new Intent(context, typeof(HttpServerService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                context.StartForegroundService(svc);
            else
                context.StartService(svc);
            NasLog.Add("BOOT", "开机自启已触发");
        }
        catch (Exception e) { NasLog.Add("BOOT", "开机自启失败: " + e.Message); }
    }
}
