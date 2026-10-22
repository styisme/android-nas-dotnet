using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using System;
using System.IO;
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
            var logDir = Path.Combine(FilesDir?.AbsolutePath ?? AppContext.BaseDirectory, "Logs");
            NasLog.Init(logDir);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex) NasLog.Crash(ex);
            };

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _statusText = FindViewById<TextView>(Resource.Id.statusText);
            _syncBtn = FindViewById<Button>(Resource.Id.syncBtn);

            if (_syncBtn != null)
            {
                // 短按是扫描，长按看日志
                _syncBtn.Click += (s, e) => 
                {
                    if (_statusText != null) _statusText.Text = "发现 NAS 位于\n192.168.1.100";
                };
                _syncBtn.LongClick += (s, e) => 
                {
                    var logs = NasLog.Snapshot();
                    var content = string.Join("\n\n", logs);
                    new AlertDialog.Builder(this)
                        .SetTitle("手表崩溃日志")
                        .SetMessage(logs.Length == 0 ? "(暂无)" : content)
                        .SetPositiveButton("好", (d, w) => { })
                        .Show();
                };
            }
        }
    }
}
