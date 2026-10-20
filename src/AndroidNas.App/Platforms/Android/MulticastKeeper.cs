using Android.Content;
using Android.Net.Wifi;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

public sealed class MulticastKeeper
{
    private WifiManager.MulticastLock? _lock;
    private readonly Context _ctx;

    public MulticastKeeper(Context ctx) { _ctx = ctx; }

    public void Acquire()
    {
        try
        {
            var wm = (WifiManager?)_ctx.ApplicationContext?.GetSystemService(Context.WifiService);
            if (wm == null) return;

            if (_lock == null)
            {
                _lock = wm.CreateMulticastLock("AndroidNAS::Multicast");
                _lock?.SetReferenceCounted(true);
            }
            if (_lock?.IsHeld == false) _lock.Acquire();
        }
        catch (Exception e)
        {
            NasLog.Add("MULTICAST", "MulticastLock 获取失败: " + e.Message);
        }
    }

    public void Release()
    {
        try { if (_lock?.IsHeld == true) _lock.Release(); } catch { }
        _lock = null;
    }
}
