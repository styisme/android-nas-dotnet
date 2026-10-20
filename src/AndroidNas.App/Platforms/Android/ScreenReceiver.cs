using Android.App;
using Android.Content;

namespace AndroidNas.App.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { Intent.ActionScreenOff, Intent.ActionScreenOn, Intent.ActionUserPresent })]
public class ScreenReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent?.Action == null) return;

        if (intent.Action == Intent.ActionScreenOff)
        {
            try
            {
                var a = new Intent(context, typeof(OnePixelActivity));
                a.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(a);
            }
            catch { }
        }
        else if (intent.Action == Intent.ActionScreenOn)
        {
            try { context.SendBroadcast(new Intent("finish_one_pixel")); }
            catch { }
        }
    }
}
