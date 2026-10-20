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
        _cpu = pm.NewWakeLock(WakeLockFlags.Partial, "AndroidNAS::CpuLock");
        _cpu.SetReferenceCounted(false);

        var wm = (WifiManager)ctx.ApplicationContext!.GetSystemService(Context.WifiService)!;
        var mode = AppSettings.LowPowerMode
            ? WifiManager.WifiLockFlags.Full
            : WifiManager.WifiLockFlags.HighPerf;
        _wifi = wm.CreateWifiLock(mode, "AndroidNAS::Wifi");
        _wifi.SetReferenceCounted(false);

        _renewHandler = new Handler(Looper.MainLooper!);
        _renew = () =>
        {
            try { if (!_cpu.IsHeld) _cpu.Acquire(SixHours); } catch { }
            _renewHandler.PostDelayed(_renew, 5 * 60 * 60 * 1000L);
        };
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
