using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidNas.Core;
using Microsoft.Maui.ApplicationModel;
using System;

namespace AndroidNas.App.Platforms.Android;

[Activity(Label = "NAS Server", MainLauncher = true,
          Theme = "@style/Maui.SplashTheme", Exported = true,
          LaunchMode = LaunchMode.SingleTop,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // 捕获所有未处理的异常，防止崩溃并写入日志
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            NasLog.Add("CRASH", "全局崩溃: " + ex?.Message + "\n" + ex?.StackTrace);
        };

        base.OnCreate(savedInstanceState);

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                RequestPermissions(new[] { global::Android.Manifest.Permission.PostNotifications }, 1001);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                if (!global::Android.OS.Environment.IsExternalStorageManager)
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
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                RequestIgnoreBatteryOptimization();

            if (!HttpServerService.IsRunning)
                ServerState.Start();
        }
        catch (Exception ex)
        {
            NasLog.Add("CRASH", "Activity 初始化异常: " + ex.Message);
        }
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
