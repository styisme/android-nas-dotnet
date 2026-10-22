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
        AppSettings.Store ??= new MauiSettingsStore();
        AuthService.Store ??= new MauiSettingsStore();
        
        // 初始化日志
        NasLog.Init();

        // 全局异常捕获
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            NasLog.Crash(e.Exception);
            e.SetObserved();
        };
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            NasLog.Crash(e.Exception);
        };

        base.OnCreate(savedInstanceState);

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                RequestPermissions(new[] { global::Android.Manifest.Permission.PostNotifications }, 1001);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R && !global::Android.OS.Environment.IsExternalStorageManager)
            {
                try
                {
                    var intent = new Intent(global::Android.Provider.Settings.ActionManageAppAllFilesAccessPermission,
                        global::Android.Net.Uri.Parse("package:" + PackageName));
                    StartActivity(intent);
                }
                catch { }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M) RequestIgnoreBatteryOptimization();

            if (!HttpServerService.IsRunning) ServerState.Start();
        }
        catch (Exception ex)
        {
            NasLog.Crash(ex);
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
