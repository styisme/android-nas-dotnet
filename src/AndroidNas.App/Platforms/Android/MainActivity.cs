using Android.App;
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
