using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using System;
using System.IO;
using AndroidNas.Core;

namespace AndroidNas.TV
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
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

            var listView = FindViewById<ListView>(Resource.Id.fileList);
            var ipText = FindViewById<TextView>(Resource.Id.ipText);

            // 长按电视屏幕显示日志
            if (listView != null)
            {
                listView.ItemLongClick += (s, e) =>
                {
                    var logs = NasLog.Snapshot();
                    var content = $"[路径: {logDir}]\n\n" + (logs.Length == 0 ? "(暂无日志)" : string.Join("\n\n", logs));
                    new AlertDialog.Builder(this)
                        .SetTitle("TV 崩溃日志")
                        .SetMessage(content)
                        .SetPositiveButton("好", (d, w) => { })
                        .Show();
                };
            }
        }
    }
}
