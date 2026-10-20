using Android.App;
using Android.Content;
using Android.OS;

namespace AndroidNas.App.Platforms.Android;

public sealed class BootAlarmScheduler
{
    private const int RequestCode = 0x4E4153; // "NAS"
    private readonly Context _ctx;

    public BootAlarmScheduler(Context ctx) => _ctx = ctx;

    public void ScheduleRepeating()
    {
        try
        {
            var am = (AlarmManager?)_ctx.GetSystemService(Context.AlarmService);
            if (am == null) return;

            var flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                flags |= PendingIntentFlags.Immutable;

            var pi = PendingIntent.GetBroadcast(_ctx, RequestCode,
                new Intent(_ctx, typeof(AlarmReceiver)), flags);

            const long interval = 5 * 60 * 1000L;
            var trigger = SystemClock.ElapsedRealtime() + interval;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                am.SetAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, trigger, pi);
            else
                am.SetRepeating(AlarmType.ElapsedRealtimeWakeup, trigger, interval, pi);
        }
        catch { }
    }

    public void Cancel()
    {
        try
        {
            var am = (AlarmManager?)_ctx.GetSystemService(Context.AlarmService);
            if (am == null) return;

            var flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                flags |= PendingIntentFlags.Immutable;

            var pi = PendingIntent.GetBroadcast(_ctx, RequestCode,
                new Intent(_ctx, typeof(AlarmReceiver)), flags);
            if (pi != null) am.Cancel(pi);
        }
        catch { }
    }
}
