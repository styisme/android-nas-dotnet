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
        
        NasLog.Init();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            NasLog.Crash(e.Exception);
            e.SetObserved();
        };
        
        // 修复：使用 global:: 强制指定全局命名空间，防止被解析到 AndroidNas.App.Platforms.Android
        global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
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
