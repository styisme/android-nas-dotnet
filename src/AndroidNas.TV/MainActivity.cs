using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using System;
using AndroidNas.Core;

namespace AndroidNas.TV
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
            };
            NasLog.Init();

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            var listView = FindViewById<ListView>(Resource.Id.fileList);
            var ipText = FindViewById<TextView>(Resource.Id.ipText);

            if (listView != null)
            {
                listView.ItemClick += (s, e) => 
                {
                    if (ipText != null)
                        Toast.MakeText(this, $"点击了: {e.Position}", ToastLength.Short)?.Show();
                };
            }
        }
    }
}
