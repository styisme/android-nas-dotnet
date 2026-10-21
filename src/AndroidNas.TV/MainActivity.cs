using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Views;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AndroidNas.TV
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private GridView _gridView;
        private TextView _ipText;
        private HttpClient _http;
        private List<string> _files = new List<string>();
        private System.Threading.CancellationTokenSource _udpCts = new();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _gridView = FindViewById<GridView>(Resource.Id.fileList);
            _ipText = FindViewById<TextView>(Resource.Id.ipText);

            _http = new HttpClient();
            _http.BaseAddress = new Uri("http://192.168.1.100:8080"); 
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:admin"));
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            _gridView.ItemClick += (s, e) => 
            {
                Toast.MakeText(this, $"点击了: {_files[e.Position]}", ToastLength.Short).Show();
            };

            LoadFiles();
            
            // 启动 UDP 监听接收投屏
            StartUdpListener();
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
                _gridView.Adapter = adapter;
                _ipText.Text = "连接成功！文件数量: " + _files.Count;
            }
            catch (Exception ex)
            {
                _ipText.Text = "连接失败: " + ex.Message;
            }
        }

        private void StartUdpListener()
        {
            Task.Run(async () =>
            {
                using var udp = new UdpClient(8090);
                udp.EnableBroadcast = true;
                while (!_udpCts.IsCancellationRequested)
                {
                    try
                    {
                        var res = await udp.ReceiveAsync(_udpCts.Token);
                        var msg = Encoding.UTF8.GetString(res.Buffer);
                        if (msg.StartsWith("PLAY:"))
                        {
                            var url = msg.Substring(5);
                            RunOnUiThread(() => {
                                _ipText.Text = "收到投屏指令，准备播放：" + url;
                                // 这里可以拉起外部播放器
                                var intent = new Intent(Intent.ActionView);
                                intent.SetDataAndType(Android.Net.Uri.Parse(url), "video/*");
                                StartActivity(intent);
                            });
                        }
                    }
                    catch { }
                }
            });
        }

        protected override void OnDestroy()
        {
            _udpCts.Cancel();
            base.OnDestroy();
        }
    }
}
