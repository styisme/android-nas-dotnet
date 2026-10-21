using Android.Content;
using Android.OS;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

public sealed class WakeLockKeeper
{
    private readonly PowerManager.WakeLock _cpu;
    // 暂不实现 WifiLock，避免各种底层 API 绑定差异
    // private readonly WifiManager.WifiLock _wifi;
    private readonly Handler _renewHandler;
    private readonly Action _renew;
    private const long SixHours = 6 * 60 * 60 * 1000L;

    public WakeLockKeeper(Context ctx)
    {
        var pm = (PowerManager)ctx.GetSystemService(Context.PowerService)!;
        _cpu = pm.NewWakeLock(WakeLockFlags.Partial, "AndroidNAS::CpuLock") ?? throw new InvalidOperationException("WakeLock failed");
        _cpu.SetReferenceCounted(false);

        // 注释掉 WifiLock 的获取，等待后续研究
        /*
        var wm = (WifiManager)ctx.ApplicationContext!.GetSystemService(Context.WifiService)!;
        var mode = AppSettings.LowPowerMode ? WifiMode.Full : WifiMode.FullHighPerf;
        _wifi = wm.CreateWifiLock(mode, "AndroidNAS::Wifi") ?? throw new InvalidOperationException("WifiLock failed");
        _wifi.SetReferenceCounted(false);
        */

        _renewHandler = new Handler(Looper.MainLooper!);
        
        var renewAction = default(Action);
        renewAction = () =>
        {
            try { if (!_cpu.IsHeld) _cpu.Acquire(SixHours); } catch { }
            if (renewAction != null)
                _renewHandler.PostDelayed(renewAction, 5 * 60 * 60 * 1000L);
        };
        _renew = renewAction;
    }

    public void Acquire()
    {
        try { _cpu.Acquire(SixHours); } catch { }
        // try { _wifi.Acquire(); } catch { }
        _renewHandler.PostDelayed(_renew, 5 * 60 * 60 * 1000L);
    }

    public void Release()
    {
        _renewHandler.RemoveCallbacks(_renew);
        try { if (_cpu.IsHeld) _cpu.Release(); } catch { }
        // try { if (_wifi.IsHeld) _wifi.Release(); } catch { }
    }
}
