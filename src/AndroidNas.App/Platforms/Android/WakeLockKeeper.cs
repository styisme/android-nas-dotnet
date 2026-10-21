using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using AndroidNas.Core;

namespace AndroidNas.App.Platforms.Android;

public sealed class WakeLockKeeper
{
    private readonly PowerManager.WakeLock _cpu;
    private readonly WifiManager.WifiLock _wifi;
    private readonly Handler _renewHandler;
    private readonly Action _renew;
    private const long SixHours = 6 * 60 * 60 * 1000L;

    public WakeLockKeeper(Context ctx)
    {
        var pm = (PowerManager)ctx.GetSystemService(Context.PowerService)!;
        _cpu = pm.NewWakeLock(WakeLockFlags.Partial, "AndroidNAS::CpuLock") ?? throw new InvalidOperationException("WakeLock failed");
        _cpu.SetReferenceCounted(false);

        var wm = (WifiManager)ctx.ApplicationContext!.GetSystemService(Context.WifiService)!;
        
        // 修复：WifiLockFlags 是独立枚举，直接使用
        var mode = AppSettings.LowPowerMode
            ? WifiLockFlags.Full
            : WifiLockFlags.HighPerf;
            
        _wifi = wm.CreateWifiLock(mode, "AndroidNAS::Wifi") ?? throw new InvalidOperationException("WifiLock failed");
        _wifi.SetReferenceCounted(false);

        _renewHandler = new Handler(Looper.MainLooper!);
        
        // 修复：捕获局部变量防止可空警告
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
        try { _wifi.Acquire(); } catch { }
        _renewHandler.PostDelayed(_renew, 5 * 60 * 60 * 1000L);
    }

    public void Release()
    {
        _renewHandler.RemoveCallbacks(_renew);
        try { if (_cpu.IsHeld) _cpu.Release(); } catch { }
        try { if (_wifi.IsHeld) _wifi.Release(); } catch { }
    }
}
