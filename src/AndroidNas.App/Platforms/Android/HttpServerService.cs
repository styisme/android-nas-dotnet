using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.App.Platforms.Android;

[Service(
    Name = "com.androidnas.next.server.HttpServerService",
    ForegroundServiceType = ForegroundService.TypeDataSync,
    Exported = false)]
public class HttpServerService : Service
{
    private const string Tag = "NasService";
    private const int NotificationId = 1001;
    private const string ChannelId = "nas_service_channel";

    public static bool IsRunning { get; private set; }
    public static string LastError { get; private set; } = "";
    public static event Action? StateChanged;

    private ServerHost? _http;
    private WakeLockKeeper? _locks;
    private SilentPlayer? _silent;
    private BootAlarmScheduler? _alarm;

    public override void OnCreate()
    {
        base.OnCreate();
        Log.Info(Tag, "OnCreate");
        GoForeground();

        AppSettings.Store ??= new MauiSettingsStore();
        AuthService.Store ??= new MauiSettingsStore();

        try
        {
            _locks = new WakeLockKeeper(this);
            _locks.Acquire();
        } catch (Exception e) { NasLog.Add("SYSTEM", "WakeLock 失败: " + e.Message); }

        LastError = "";
        try
        {
            _http = new ServerHost();
            _http.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);
        }
        catch (Exception e)
        {
            LastError = "Web: " + e.Message;
            NasLog.Add("SYSTEM", "ServerHost 崩溃: " + e.Message);
        }

        try
        {
            if (AppSettings.SilentKeepAlive)
            {
                _silent = new SilentPlayer();
                _silent.Start();
            }
        } catch { }

        try
        {
            _alarm = new BootAlarmScheduler(this);
            _alarm.ScheduleRepeating();
        } catch { }

        IsRunning = _http != null;
        StateChanged?.Invoke();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        => StartCommandResult.Sticky;

    public override void OnDestroy()
    {
        try { _alarm?.Cancel(); } catch { }
        try { _silent?.Stop(); } catch { }
        try { _http?.Dispose(); } catch { }
        try { _locks?.Release(); } catch { }
        IsRunning = false;
        StateChanged?.Invoke();
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;

    private void GoForeground()
    {
        var nm = (NotificationManager)GetSystemService(NotificationService)!;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var ch = new NotificationChannel(ChannelId, "NAS 后台服务", NotificationImportance.Low)
            {
                Description = "Android NAS 正在运行"
            };
            nm.CreateNotificationChannel(ch);
        }

        var flags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            flags |= PendingIntentFlags.Immutable;

        var pi = PendingIntent.GetActivity(this, 0,
            new Intent(this, typeof(MainActivity)), flags);

        var text = "HTTP:" + AppSettings.WebPort + "  FTP:" + AppSettings.FtpPort + "  DAV:" + AppSettings.WebDavPort;

        Notification.Builder builder;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            builder = new Notification.Builder(this, ChannelId);
        else
            builder = new Notification.Builder(this);

        var notification = builder
            .SetContentTitle("Android NAS")
            .SetContentText(text)
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownloadDone)
            .SetContentIntent(pi)
            .SetOngoing(true)
            .Build();

        StartForeground(NotificationId, notification);
    }
}
