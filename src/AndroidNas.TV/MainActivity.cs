using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Views;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System;

namespace AndroidNas.TV
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private ListView _listView;
        private TextView _ipText;
        private HttpClient _http;
        private List<string> _files = new List<string>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _listView = FindViewById<ListView>(Resource.Id.fileList);
            _ipText = FindViewById<TextView>(Resource.Id.ipText);

            _http = new HttpClient();
            // 这里为了演示 TV 端的连通性，暂时连接本机或局域网测试
            _http.BaseAddress = new Uri("http://192.168.1.100:8080"); 
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:admin"));
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            _listView.ItemClick += (s, e) => 
            {
                Toast.MakeText(this, $"点击了: {_files[e.Position]}", ToastLength.Short).Show();
            };

            LoadFiles();
        }

        private async void LoadFiles()
        {
            try
            {
                var res = await _http.GetStringAsync("/api/list?path=/");
                var doc = JsonDocument.Parse(res);
                _files.Clear();
                foreach (var el in doc.RootElement.GetProperty("files").EnumerateArray())
                {
                    var dir = el.GetProperty("dir").GetBoolean() ? "[目录] " : "[文件] ";
                    _files.Add(dir + el.GetProperty("name").GetString());
                }
                
                var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, _files);
                _listView.Adapter = adapter;
                _ipText.Text = "连接成功！文件数量: " + _files.Count;
            }
            catch (Exception ex)
            {
                _ipText.Text = "请先在设置中连接 NAS。错误信息: " + ex.Message;
            }
        }
    }
}
