using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Support.Wearable.Views; // 如果存在
using System;

namespace AndroidNas.Watch
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private TextView _statusText;
        private Button _syncBtn;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _statusText = FindViewById<TextView>(Resource.Id.statusText);
            _syncBtn = FindViewById<Button>(Resource.Id.syncBtn);

            _syncBtn.Click += (s, e) => 
            {
                // 模拟扫描局域网
                _statusText.Text = "发现 NAS 位于\n192.168.1.100\n\n点击屏幕开始浏览";
            };
        }
    }
}
