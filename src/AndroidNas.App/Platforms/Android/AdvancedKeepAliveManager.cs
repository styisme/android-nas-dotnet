using Android.Content;
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
            $"printf 'id=android_nas_priv\\nname=Android NAS Next Privilege\\nversion=v1.0\\nversionCode=1\\nauthor=AndroidNAS\\ndescription=Make Android NAS Next a System Privileged App\\n' > {dir}module.prop\n" +
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
