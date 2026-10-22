using Android.Content;

namespace AndroidNas.TV
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class TvReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            // 占位，防止可空性警告
        }
    }
}
