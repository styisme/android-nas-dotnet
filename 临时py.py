#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Android NAS · 第二批：保活层 + ForegroundService + search/dlna 端点
用法:
    cd /storage/emulated/0/AndroidNas.Net
    python3 scaffold_p2.py          # 在当前目录生成
    # 或者
    python3 scaffold_p2.py --out /storage/emulated/0/AndroidNas.Net
"""

import argparse, pathlib, sys

FILES: dict[str, str] = {}

# ============================================================
# 1) ForegroundService —— 核心服务托管
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/HttpServerService.cs"] = r"""using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AndroidNas.Core;
using AndroidNas.Server;
using AndroidNas.Server.Protocols;

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
            _http.StartHttp(AppSettings.WebPort);
            NasLog.Add("127.0.0.1", $"Web 服务已启动: {AppSettings.WebPort}");
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

        IsRunning = _http is not null;
        StateChanged?.Invoke();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        => StartCommandResult.Sticky;

    public override void OnDestroy()
    {
        Log.Info(Tag, "OnDestroy");
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

        var builder = new Notification.Builder(this)
            .SetContentTitle("Android NAS")
            .SetContentText("文件服务运行中")
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownloadDone)
            .SetContentIntent(pi)
            .SetOngoing(true);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            builder.SetChannelId(ChannelId);

        StartForeground(NotificationId, builder.Build());
    }
}
"""

# ============================================================
# 2) WakeLock + WifiLock 管理
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/WakeLockKeeper.cs"] = r"""using Android.Content;
using Android.OS;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

public sealed class WakeLockKeeper
{
    private readonly PowerManager.WakeLock _cpu;
    private readonly WifiManager.WifiLock _wifi;
    private readonly Handler _renewHandler;
    private readonly Action _renew;
    private const long SixHours = 6 * 60 * 60 * 1000L;

    public WakeLockKeeper(Context ctx)
    {
        var pm = (PowerManager)ctx.GetSystemService(Context.PowerService)!;
        _cpu = pm.NewWakeLock(WakeLockFlags.Partial, "AndroidNAS::CpuLock");
        _cpu.SetReferenceCounted(false);

        var wm = (WifiManager)ctx.ApplicationContext!.GetSystemService(Context.WifiService)!;
        var mode = AppSettings.LowPowerMode
            ? WifiManager.WifiLockFlags.Full
            : WifiManager.WifiLockFlags.HighPerf;
        _wifi = wm.CreateWifiLock(mode, "AndroidNAS::Wifi");
        _wifi.SetReferenceCounted(false);

        _renewHandler = new Handler(Looper.MainLooper!);
        _renew = () =>
        {
            try { if (!_cpu.IsHeld) _cpu.Acquire(SixHours); } catch { }
            _renewHandler.PostDelayed(_renew, 5 * 60 * 60 * 1000L);
        };
    }

    public void Acquire()
    {
        try { _cpu.Acquire(SixHours); } catch { }
        try { _wifi.Acquire(); } catch { }
        _renewHandler.PostDelayed(_renew, 5 * 60 * 60 * 1000L);
    }

    public void Release()
    {
        _renewHandler.RemoveCallbacks(_renew);
        try { if (_cpu.IsHeld) _cpu.Release(); } catch { }
        try { if (_wifi.IsHeld) _wifi.Release(); } catch { }
    }
}
"""

# ============================================================
# 3) SilentPlayer —— AudioTrack 静音保活
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/SilentPlayer.cs"] = r"""using Android.Media;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

public sealed class SilentPlayer
{
    private volatile bool _running;
    private AudioTrack? _track;
    private Thread? _thread;

    public void Start()
    {
        _running = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "SilentPlayer" };
        _thread.Start();
    }

    private void Run()
    {
        try
        {
            var minBuf = AudioTrack.GetMinBufferSize(
                8000,
                ChannelOut.Mono,
                Android.Media.Encoding.Pcm16bit);
            if (minBuf <= 0) minBuf = 2048;

            _track = new AudioTrack(
                Stream.Music, 8000, ChannelOut.Mono,
                Android.Media.Encoding.Pcm16bit, minBuf, AudioTrackMode.Stream);
            _track.Play();

            var silence = new byte[minBuf];
            while (_running)
            {
                try { _track.Write(silence, 0, silence.Length); }
                catch { break; }
                Thread.Sleep(1000);
            }
        }
        catch (Exception e)
        {
            NasLog.Add("KEEPALIVE", "SilentPlayer 失败: " + e.Message);
        }
    }

    public void Stop()
    {
        _running = false;
        try { _track?.Stop(); } catch { }
        try { _track?.Release(); } catch { }
        _track = null;
    }
}
"""

# ============================================================
# 4) BootAlarmScheduler —— AlarmManager 定时唤醒
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/BootAlarmScheduler.cs"] = r"""using Android.App;
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
"""

# ============================================================
# 5) Broadcast Receivers
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/BootReceiver.cs"] = r"""using Android.App;
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
"""

FILES["src/AndroidNas.App/Platforms/Android/AlarmReceiver.cs"] = r"""using Android.App;
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
"""

FILES["src/AndroidNas.App/Platforms/Android/ScreenReceiver.cs"] = r"""using Android.App;
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
"""

# ============================================================
# 6) OnePixelActivity —— 1px 锁屏保活
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/OnePixelActivity.cs"] = r"""using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;

namespace AndroidNas.App.Platforms.Android;

[Activity(
    Name = "com.androidnas.OnePixelActivity",
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
"""

# ============================================================
# 7) AdvancedKeepAliveManager —— Root 保活
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/AdvancedKeepAliveManager.cs"] = r"""using Android.Content;
using Android.OS;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

public static class AdvancedKeepAliveManager
{
    private static readonly string[] Killers = new[]
    {
        "com.miui.powerkeeper",
        "com.huawei.powergenie",
        "com.coloros.safecenter",
        "com.vivo.permissionmanager",
        "com.samsung.android.lool"
    };

    public static bool HasRoot()
    {
        try { return RootShell.Exec("id").Contains("uid=0"); }
        catch { return false; }
    }

    public static bool IsDozeWhitelisted(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M) return true;
        try
        {
            var pm = (PowerManager)ctx.GetSystemService(Context.PowerService)!;
            return pm.IsIgnoringBatteryOptimizations(ctx.PackageName!);
        }
        catch { return false; }
    }

    public static void ApplyExtremeWhitelist(Context ctx)
    {
        if (!HasRoot()) return;

        var pkg = ctx.PackageName!;
        var pid = global::Android.OS.Process.MyPid();

        Run($"echo -1000 > /proc/{pid}/oom_score_adj");
        Run($"am set-standby-bucket {pkg} active");
        Run($"cmd appops set {pkg} RUN_IN_BACKGROUND allow");
        Run($"cmd appops set {pkg} RUN_ANY_IN_BACKGROUND allow");
        Run($"cmd deviceidle whitelist +{pkg}");

        foreach (var k in Killers)
            Run($"pm disable-user --user 0 {k}");

        NasLog.Add("KEEPALIVE", "Root 极致保活已注入");
    }

    public static void RestoreSystemServices()
    {
        if (!HasRoot()) return;
        foreach (var k in Killers) Run($"pm enable {k}");
        NasLog.Add("KEEPALIVE", "系统服务已还原");
    }

    public static bool GenerateMagiskModule(Context ctx)
    {
        if (!HasRoot()) return false;

        var pkg = ctx.PackageName!;
        var apkPath = ctx.PackageCodePath!;
        var dir = "/data/adb/modules/android_nas_priv/";
        var cmd =
            $"mkdir -p {dir}system/priv-app/AndroidNAS\n" +
            $"cp {apkPath} {dir}system/priv-app/AndroidNAS/AndroidNAS.apk\n" +
            $"printf 'id=android_nas_priv\\nname=Android NAS Privilege\\nversion=v1.0\\nversionCode=1\\nauthor=AndroidNAS\\ndescription=Make Android NAS a System Privileged App\\n' > {dir}module.prop\n" +
            $"touch {dir}auto_mount\n" +
            $"chmod -R 755 {dir}";

        Run(cmd);
        NasLog.Add("KEEPALIVE", "Magisk 模块已生成，重启手机生效");
        return true;
    }

    private static void Run(string cmd)
    {
        try { RootShell.Exec(cmd); } catch { }
    }
}
"""

# ============================================================
# 8) ServerState —— 重构：委托给 ForegroundService
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/ServerState.cs"] = r"""using Android.Content;
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
"""

# ============================================================
# 9) Endpoints：search + dlna description
# ============================================================
FILES["src/AndroidNas.Server/Endpoints/SearchEndpoints.cs"] = r"""using System.Security;
using AndroidNas.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AndroidNas.Server.Endpoints;

public static class SearchEndpoints
{
    private const int MaxResults = 200;
    private const int MaxDepth = 12;

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", (string? q, IFileRootProvider root) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.Ok(Array.Empty<object>());

            string rootFull;
            try { rootFull = root.Resolve("/"); }
            catch (SecurityException) { return Results.BadRequest("Invalid root"); }

            var needle = q.ToLowerInvariant();
            var results = new List<FileEntryDto>();

            void Walk(string dir, int depth)
            {
                if (depth > MaxDepth || results.Count >= MaxResults) return;
                IEnumerable<FileSystemInfo> children;
                try { children = new DirectoryInfo(dir).EnumerateFileSystemInfos(); }
                catch { return; }

                foreach (var e in children)
                {
                    if (results.Count >= MaxResults) return;
                    if (e.Name.ToLowerInvariant().Contains(needle))
                    {
                        var isDir = e is DirectoryInfo;
                        long size = 0;
                        if (!isDir && e is FileInfo fi) try { size = fi.Length; } catch { }
                        results.Add(new FileEntryDto(e.Name, isDir, size,
                            !isDir && MediaTypeHelper.IsTxt(e.Name),
                            !isDir && MediaTypeHelper.IsMedia(e.Name),
                            !isDir && MediaTypeHelper.IsZip(e.Name)));
                    }
                    if (e is DirectoryInfo sub)
                        Walk(sub.FullName, depth + 1);
                }
            }

            Walk(rootFull, 0);
            return Results.Ok(results);
        });

        return app;
    }
}
"""

FILES["src/AndroidNas.Server/Endpoints/DlnaEndpoints.cs"] = r"""using AndroidNas.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AndroidNas.Server.Endpoints;

public static class DlnaEndpoints
{
    public static IEndpointRouteBuilder MapDlnaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dlna/description.xml", (HttpContext ctx) =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var alias = System.Security.SecurityElement.Escape(AppSettings.DeviceAlias) ?? "Android NAS";
            var uuid = AppSettings.Fingerprint;

            var xml =
                "<?xml version=\"1.0\"?>" +
                "<root xmlns=\"urn:schemas-upnp-org:device-1-0\">" +
                "<specVersion><major>1</major><minor>0</minor></specVersion>" +
                "<device>" +
                "<deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>" +
                "<friendlyName>" + alias + "</friendlyName>" +
                "<manufacturer>AndroidNAS</manufacturer>" +
                "<modelName>Android NAS</modelName>" +
                "<modelNumber>4.0</modelNumber>" +
                "<UDN>uuid:" + uuid + "</UDN>" +
                "<serviceList>" +
                "<service>" +
                "<serviceType>urn:schemas-upnp-org:service:ContentDirectory:1</serviceType>" +
                "<serviceId>urn:upnp-org:serviceId:ContentDirectory</serviceId>" +
                "<controlURL>/dlna/control</controlURL>" +
                "<eventSubURL>/dlna/event</eventSubURL>" +
                "<SCPDURL>/dlna/content-directory.xml</SCPDURL>" +
                "</service>" +
                "</serviceList>" +
                "</device>" +
                "</root>";

            return Results.Text(xml, "application/xml", System.Text.Encoding.UTF8);
        });

        app.MapGet("/dlna/content-directory.xml", () =>
        {
            const string scpd =
                "<?xml version=\"1.0\"?>" +
                "<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\">" +
                "<specVersion><major>1</major><minor>0</minor></specVersion>" +
                "<actionList><action>" +
                "<name>Browse</name>" +
                "<argumentList>" +
                "<argument><name>ObjectID</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_ObjectID</relatedStateVariable></argument>" +
                "<argument><name>BrowseFlag</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_BrowseFlag</relatedStateVariable></argument>" +
                "<argument><name>Filter</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_Filter</relatedStateVariable></argument>" +
                "<argument><name>StartingIndex</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_Index</relatedStateVariable></argument>" +
                "<argument><name>RequestedCount</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable></argument>" +
                "<argument><name>SortCriteria</name><direction>in</direction><relatedStateVariable>A_ARG_TYPE_SortCriteria</relatedStateVariable></argument>" +
                "<argument><name>Result</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_Result</relatedStateVariable></argument>" +
                "<argument><name>NumberReturned</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable></argument>" +
                "<argument><name>TotalMatches</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable></argument>" +
                "<argument><name>UpdateID</name><direction>out</direction><relatedStateVariable>A_ARG_TYPE_UpdateID</relatedStateVariable></argument>" +
                "</argumentList>" +
                "</action></actionList>" +
                "<serviceStateTable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_ObjectID</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_BrowseFlag</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Filter</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Index</name><dataType>ui4</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Count</name><dataType>ui4</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_SortCriteria</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_Result</name><dataType>string</dataType></stateVariable>" +
                "<stateVariable sendEvents=\"no\"><name>A_ARG_TYPE_UpdateID</name><dataType>ui4</dataType></stateVariable>" +
                "</serviceStateTable>" +
                "</scpd>";
            return Results.Text(scpd, "application/xml", System.Text.Encoding.UTF8);
        });

        return app;
    }
}
"""

# ============================================================
# 10) ServerHost 修补 —— 注册 search + dlna
# ============================================================
FILES["src/AndroidNas.Server/ServerHost.cs"] = r"""using AndroidNas.Core;
using AndroidNas.Server.Auth;
using AndroidNas.Server.Endpoints;
using AndroidNas.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace AndroidNas.Server;

public sealed class ServerHost : IDisposable
{
    private WebApplication? _app;

    public void StartHttp(int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        builder.Services.AddSingleton<IAuthConfig, DefaultAuthConfig>();
        builder.Services.AddSingleton<IFileRootProvider>(_ =>
            new PhysicalRootProvider(AppSettings.RootPath, AppSettings.GlobalMode));

        var app = builder.Build();
        app.UseMiddleware<BasicAuthMiddleware>();

        var wwwRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (Directory.Exists(wwwRoot))
        {
            var fp = new PhysicalFileProvider(wwwRoot);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fp });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = fp });
        }

        app.MapSystemEndpoints();
        app.MapFileEndpoints();
        app.MapShareEndpoints();
        app.MapSearchEndpoints();
        app.MapDlnaEndpoints();

        _app = app;
        _ = app.StartAsync();
        NasLog.Add("127.0.0.1", $"Web 服务已启动: {port}");
    }

    public void Dispose()
    {
        try { _app?.StopAsync().GetAwaiter().GetResult(); } catch { }
    }
}
"""

# ============================================================
# 11) MainActivity 修补 —— 启动 Service + 触发权限
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/MainActivity.cs"] = r"""using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidNas.Core;
using Microsoft.Maui.ApplicationModel;

namespace AndroidNas.App.Platforms.Android;

[Activity(Label = "__NAME__", MainLauncher = true,
          Theme = "@style/Maui.SplashTheme", Exported = true,
          LaunchMode = LaunchMode.SingleTop,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            RequestPermissions(new[] { global::Android.Manifest.Permission.PostNotifications }, 1001);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R &&
            !global::Android.OS.Environment.IsExternalStorageManager)
        {
            try
            {
                var intent = new Intent(
                    global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission,
                    global::Android.Net.Uri.Parse("package:" + PackageName));
                StartActivity(intent);
            }
            catch { }
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            RequestIgnoreBatteryOptimization();

        if (!HttpServerService.IsRunning)
            ServerState.Start();
    }

    private void RequestIgnoreBatteryOptimization()
    {
        try
        {
            var pm = (PowerManager)GetSystemService(PowerService)!;
            if (pm.IsIgnoringBatteryOptimizations(PackageName!)) return;
            var intent = new Intent(global::Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations,
                global::Android.Net.Uri.Parse("package:" + PackageName));
            StartActivity(intent);
        }
        catch { }
    }
}
"""

# ============================================================
# 12) AndroidManifest 修补
# ============================================================
FILES["src/AndroidNas.App/Platforms/Android/AndroidManifest.xml"] = r"""<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <uses-permission android:name="android.permission.INTERNET" />
  <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
  <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
  <uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
  <uses-permission android:name="android.permission.WAKE_LOCK" />
  <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
  <uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
  <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
  <uses-permission android:name="android.permission.FOREGROUND_SERVICE_DATA_SYNC" />
  <uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS" />
  <uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE" />
  <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
  <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" android:maxSdkVersion="28" />

  <application
      android:usesCleartextTraffic="true"
      android:requestLegacyExternalStorage="true"
      android:enableOnBackInvokedCallback="true" />
</manifest>
"""

# ============================================================
# 13) KeepAlivePage —— 高级保活 UI
# ============================================================
FILES["src/AndroidNas.App/Pages/KeepAlivePage.xaml"] = r"""<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="AndroidNas.App.Pages.KeepAlivePage"
             Title="高级保活"
             BackgroundColor="{StaticResource MdSurface}">
    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="12">
            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="6">
                    <Label Text="环境检测" Style="{StaticResource MdTitleMedium}" />
                    <Label x:Name="RootLabel" Text="Root: 检测中…" Style="{StaticResource MdBodyMedium}" />
                    <Label x:Name="DozeLabel" Text="Doze 白名单: 检测中…" Style="{StaticResource MdBodyMedium}" />
                    <Label x:Name="ServiceLabel" Text="服务: 检测中…" Style="{StaticResource MdBodyMedium}" />
                </VerticalStackLayout>
            </Border>

            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="极致保活 (需 Root)" Style="{StaticResource MdTitleMedium}" />
                    <Label Text="OOM -1000 免死 + 系统分桶 + 冻结厂商省电服务"
                           Style="{StaticResource MdBodyMedium}" />
                    <Button Text="一键免死注入" Style="{StaticResource MdButtonFilled}"
                            Clicked="OnApplyExtreme" />
                    <Button Text="生成 Magisk 特权模块" Style="{StaticResource MdButtonTonal}"
                            Clicked="OnGenerateMagisk" />
                    <Button Text="还原系统服务" Style="{StaticResource MdButtonTonal}"
                            Clicked="OnRestoreServices" />
                </VerticalStackLayout>
            </Border>

            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="6">
                    <Label Text="最近保活日志" Style="{StaticResource MdTitleMedium}" />
                    <Label x:Name="LogLabel" Text="(暂无)" Style="{StaticResource MdBodyMedium}" />
                </VerticalStackLayout>
            </Border>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
"""

FILES["src/AndroidNas.App/Pages/KeepAlivePage.xaml.cs"] = r"""using AndroidNas.Core;

namespace AndroidNas.App.Pages;

public partial class KeepAlivePage : ContentPage
{
    public KeepAlivePage() { InitializeComponent(); }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

#if ANDROID
    private void Refresh()
    {
        var ctx = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        var root = Platforms.Android.AdvancedKeepAliveManager.HasRoot();
        var doze = Platforms.Android.AdvancedKeepAliveManager.IsDozeWhitelisted(ctx);
        var running = Platforms.Android.ServerState.IsRunning;

        RootLabel.Text = "Root: " + (root ? "✅ 已获取" : "❌ 未获取");
        DozeLabel.Text = "Doze 白名单: " + (doze ? "✅ 已加入" : "❌ 未加入");
        ServiceLabel.Text = "服务: " + (running ? "✅ 运行中" : "❌ 已停止");

        var logs = NasLog.Snapshot();
        LogLabel.Text = logs.Length == 0 ? "(暂无)" : string.Join("\n", logs.Take(8));
    }

    private async void OnApplyExtreme(object sender, EventArgs e)
    {
        var ctx = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        if (!Platforms.Android.AdvancedKeepAliveManager.HasRoot())
        {
            await DisplayAlert("需要 Root", "当前设备未获取 Root 权限。", "知道了");
            return;
        }
        await Task.Run(() => Platforms.Android.AdvancedKeepAliveManager.ApplyExtremeWhitelist(ctx));
        Refresh();
        await DisplayAlert("完成", "极致保活注入完成。", "好");
    }

    private async void OnGenerateMagisk(object sender, EventArgs e)
    {
        var ctx = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        if (!Platforms.Android.AdvancedKeepAliveManager.HasRoot())
        {
            await DisplayAlert("需要 Root", "当前设备未获取 Root 权限。", "知道了");
            return;
        }
        var ok = await Task.Run(() => Platforms.Android.AdvancedKeepAliveManager.GenerateMagiskModule(ctx));
        await DisplayAlert("Magisk 模块", ok ? "已生成，重启手机后生效。" : "生成失败。", "好");
    }

    private async void OnRestoreServices(object sender, EventArgs e)
    {
        await Task.Run(() => Platforms.Android.AdvancedKeepAliveManager.RestoreSystemServices());
        await DisplayAlert("完成", "厂商系统服务已还原。", "好");
    }
#else
    private void Refresh() { }
    private void OnApplyExtreme(object sender, EventArgs e) { }
    private void OnGenerateMagisk(object sender, EventArgs e) { }
    private void OnRestoreServices(object sender, EventArgs e) { }
#endif
}
"""

# ============================================================
# 14) HomePage 补一个"高级保活"入口
# ============================================================
FILES["src/AndroidNas.App/Pages/HomePage.xaml"] = r"""<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="AndroidNas.App.Pages.HomePage"
             BackgroundColor="{StaticResource MdSurface}">
    <Grid RowDefinitions="Auto,*">
        <VerticalStackLayout Grid.Row="0" Padding="16,12" Spacing="2">
            <Label Text="Android NAS" Style="{StaticResource MdHeadline}" />
            <Label x:Name="Subtitle" Text="准备就绪" Style="{StaticResource MdBodyMedium}" />
        </VerticalStackLayout>

        <ScrollView Grid.Row="1">
            <VerticalStackLayout Padding="16" Spacing="12">
                <Border Style="{StaticResource MdCard}">
                    <Grid RowDefinitions="Auto,Auto" ColumnDefinitions="*,Auto">
                        <VerticalStackLayout>
                            <Label Text="文件服务" Style="{StaticResource MdTitleMedium}" />
                            <Label x:Name="ServiceStatusLabel" Text="已停止" Style="{StaticResource MdBodyMedium}" />
                        </VerticalStackLayout>
                        <Button x:Name="ToggleBtn" Grid.Column="1"
                                Text="启动" Style="{StaticResource MdButtonFilled}"
                                Clicked="OnToggleService" VerticalOptions="Center" />
                        <Label x:Name="AddressLabel" Grid.Row="1" Grid.ColumnSpan="2"
                               Text="http://127.0.0.1:8080"
                               Style="{StaticResource MdBodyMedium}"
                               Margin="0,12,0,0" />
                    </Grid>
                </Border>

                <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                    <Button Text="🌐 打开 Web" Style="{StaticResource MdButtonTonal}" Clicked="OnOpenWeb" />
                    <Button Grid.Column="1" Text="🔁 刷新" Style="{StaticResource MdButtonTonal}" Clicked="OnRefresh" />
                </Grid>

                <Border Style="{StaticResource MdCard}" Margin="0,8,0,0">
                    <VerticalStackLayout Spacing="8">
                        <Label Text="后台保活" Style="{StaticResource MdTitleMedium}" />
                        <Label x:Name="KeepAliveStatus" Text="检测中…" Style="{StaticResource MdBodyMedium}" />
                        <Button Text="高级保活设置" Style="{StaticResource MdButtonTonal}"
                                Clicked="OnOpenKeepAlive" />
                    </VerticalStackLayout>
                </Border>
            </VerticalStackLayout>
        </ScrollView>
    </Grid>
</ContentPage>
"""

FILES["src/AndroidNas.App/Pages/HomePage.xaml.cs"] = r"""#if ANDROID
using AndroidNas.App.Platforms.Android;
using AndroidNas.Core;
#endif

namespace AndroidNas.App.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
#if ANDROID
        ServerState.Changed += () => MainThread.BeginInvokeOnMainThread(Refresh);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
#if ANDROID
        var running = ServerState.IsRunning;
        ServiceStatusLabel.Text = running ? "运行中" : "已停止";
        ToggleBtn.Text = running ? "停止" : "启动";
        AddressLabel.Text = $"http://{NetworkHelper.GetLocalIp()}:{AppSettings.WebPort}";
        Subtitle.Text = running ? "服务运行中 · v4.0" : "服务已停止 · v4.0";

        var root = AdvancedKeepAliveManager.HasRoot();
        var doze = AdvancedKeepAliveManager.IsDozeWhitelisted(Microsoft.Maui.ApplicationModel.Platform.AppContext);
        KeepAliveStatus.Text =
            $"Root: {(root ? "✅" : "❌")}   Doze: {(doze ? "✅" : "❌")}   静音保活: {(AppSettings.SilentKeepAlive ? "✅" : "❌")}";
#endif
    }

    private async void OnToggleService(object sender, EventArgs e)
    {
#if ANDROID
        await ServerState.ToggleAsync();
        await Task.Delay(400);
        Refresh();
#else
        await Task.CompletedTask;
#endif
    }

    private void OnRefresh(object sender, EventArgs e) => Refresh();

    private async void OnOpenWeb(object sender, EventArgs e)
    {
#if ANDROID
        try { await Launcher.Default.OpenAsync($"http://127.0.0.1:{AppSettings.WebPort}"); }
        catch { }
#else
        await Task.CompletedTask;
#endif
    }

    private async void OnOpenKeepAlive(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new KeepAlivePage());
    }
}
"""

# ============================================================
# 15) AppShell 修补 —— 注册保活页路由
# ============================================================
FILES["src/AndroidNas.App/AppShell.xaml.cs"] = r"""namespace AndroidNas.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("keepalive", typeof(Pages.KeepAlivePage));
    }
}
"""


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=".")
    parser.add_argument("--force", action="store_true", help="覆盖目标目录里非本批次的冲突文件（默认会覆盖）")
    args = parser.parse_args()

    root = pathlib.Path(args.out).resolve()
    if not root.exists():
        print(f"目标目录不存在: {root}")
        print("请先跑第一批 scaffold.py，或用 --out 指定正确路径。")
        return 1

    # 检查是不是我们的项目
    marker = root / "src" / "AndroidNas.App" / "AndroidNas.App.csproj"
    if not marker.exists():
        print(f"未在 {root} 找到 src/AndroidNas.App/AndroidNas.App.csproj")
        print("请确认这是第一批生成的项目根目录。")
        return 1

    print(f"目标根目录: {root}")
    print()

    for rel, content in FILES.items():
        # 保留 __NAME__ __PKG__ 占位符替换（HomePage/MainActivity 里没用到，但兼容）
        text = content
        target = root / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8")
        print(f"  ✔ {rel}")

    print()
    print("=" * 60)
    print("第二批生成完成")
    print("=" * 60)
    print()
    print("本批新增/改动:")
    print("  [架构]  服务迁移到 ForegroundService，App 关闭后仍运行")
    print("  [保活]  WakeLock + WifiLock + SilentPlayer + OnePixel + Alarm 唤醒")
    print("  [自启]  开机自启 (BOOT_COMPLETED) + 应用更新后自启")
    print("  [Root]  OOM -1000 / Magisk 模块 / 厂商省电服务冻结")
    print("  [API]   新增 /api/search  /dlna/description.xml  /dlna/content-directory.xml")
    print("  [UI]    新增 KeepAlivePage + 首页保活状态")
    print()
    print("下一步编译:")
    print(f"  cd {root}")
    print("  dotnet build src/AndroidNas.App -f net10.0-android -c Debug")
    print()
    print("首次装机注意:")
    print("  - 允许通知权限 (Android 13+)")
    print("  - 允许所有文件访问权限 (Android 11+)")
    print("  - 忽略电池优化 (Android 6+)")
    print("  - App 启动后自动拉起 ForegroundService，状态栏应显示 Android NAS 通知")
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())