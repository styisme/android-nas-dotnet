using Android.Content;
using Android.App;
using Android.Widget;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AndroidNas.TV
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class TvReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            // 这里可以在后台监听 UDP，但由于我们用了原生 Socket，直接在 MainActivity 启动个线程监听
        }
    }
}
