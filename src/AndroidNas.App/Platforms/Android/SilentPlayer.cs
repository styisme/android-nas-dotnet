using Android.Media;
using AndroidNas.Core;
using System.Threading;

namespace AndroidNas.App.Platforms.Android;

public sealed class SilentPlayer
{
    private volatile bool _running;
    private AudioTrack? _track;
    private Thread? _thread;

    public void Start()
    {
        _running = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "SilentPlayer" };
        _thread.Start();
    }

    private void Run()
    {
        try
        {
            var minBuf = AudioTrack.GetMinBufferSize(
                8000,
                ChannelConfiguration.Mono,
                Android.Media.Encoding.Pcm16bit);
            if (minBuf <= 0) minBuf = 2048;

            _track = new AudioTrack(
                Android.Media.Stream.Music, 8000, ChannelConfiguration.Mono,
                Android.Media.Encoding.Pcm16bit, minBuf, AudioTrackMode.Stream);
            _track.Play();

            var silence = new byte[minBuf];
            while (_running)
            {
                try { _track.Write(silence, 0, silence.Length); }
                catch { break; }
                Thread.Sleep(1000);
            }
        }
        catch (Exception e)
        {
            NasLog.Add("KEEPALIVE", "SilentPlayer 失败: " + e.Message);
        }
    }

    public void Stop()
    {
        _running = false;
        try { _track?.Stop(); } catch { }
        try { _track?.Release(); } catch { }
        _track = null;
    }
}
