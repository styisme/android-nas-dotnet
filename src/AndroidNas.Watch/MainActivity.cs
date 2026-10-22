using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using System;
using AndroidNas.Core;

namespace AndroidNas.Watch
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private TextView? _statusText;
        private Button? _syncBtn;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
            };
            NasLog.Init();

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _statusText = FindViewById<TextView>(Resource.Id.statusText);
            _syncBtn = FindViewById<Button>(Resource.Id.syncBtn);

            if (_syncBtn != null)
            {
                _syncBtn.Click += (s, e) => 
                {
                    if (_statusText != null) _statusText.Text = "发现 NAS 位于\n192.168.1.100";
                };
            }
        }
    }
}
