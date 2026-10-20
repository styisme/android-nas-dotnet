using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;

namespace AndroidNas.App.Platforms.Android;

[Activity(
    Name = "com.androidnas.next.OnePixelActivity",
    Theme = "@android:style/Theme.Translucent.NoTitleBar",
    ExcludeFromRecents = true,
    LaunchMode = global::Android.Content.PM.LaunchMode.SingleInstance)]
public class OnePixelActivity : Activity
{
    private readonly FinishReceiver _finishReceiver = new();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window != null)
        {
            Window.SetLayout(1, 1);
            Window.SetGravity(GravityFlags.Top | GravityFlags.Start);
            Window.AddFlags(WindowManagerFlags.NotTouchable | WindowManagerFlags.NotFocusable);
        }

        var filter = new IntentFilter("finish_one_pixel");
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            RegisterReceiver(_finishReceiver, filter, ReceiverFlags.NotExported);
        else
            RegisterReceiver(_finishReceiver, filter);
    }

    protected override void OnDestroy()
    {
        try { UnregisterReceiver(_finishReceiver); } catch { }
        base.OnDestroy();
    }

    private class FinishReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            try { ((Activity?)context)?.Finish(); } catch { }
        }
    }
}
