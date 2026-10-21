using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.App.Platforms.Android;

[Service(
    Name = "com.androidnas.HttpServerService",
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

        _locks = new WakeLockKeeper(this);
        _locks.Acquire();

        LastError = "";
        try
        {
            _http = new ServerHost();
            _http.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);
        }
        catch (Exception e)
        {
            LastError = "Web: " + e.Message;
            NasLog.Add("127.0.0.1", "Web 启动失败: " + e.Message);
        }

        if (AppSettings.SilentKeepAlive)
        {
            _silent = new SilentPlayer();
            _silent.Start();
        }

        _alarm = new BootAlarmScheduler(this);
        _alarm.ScheduleRepeating();

        IsRunning = _http != null;
        StateChanged?.Invoke();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        => StartCommandResult.Sticky;

    public override void OnDestroy()
    {
        _alarm?.Cancel();
        _silent?.Stop();
        _http?.Dispose();
        _locks?.Release();
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

        var text = "HTTP:" + AppSettings.WebPort +
                   "  FTP:" + AppSettings.FtpPort +
                   "  DAV:" + AppSettings.WebDavPort;

        // 修复过时警告，使用 NotificationCompat.Builder 或者根据版本处理
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
