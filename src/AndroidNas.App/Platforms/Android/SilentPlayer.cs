using AndroidNas.Core;
using System.Threading;

namespace AndroidNas.App.Platforms.Android;

public sealed class SilentPlayer
{
    private volatile bool _running;
    private global::Android.Media.AudioTrack? _track;
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
            var minBuf = global::Android.Media.AudioTrack.GetMinBufferSize(
                8000,
                global::Android.Media.ChannelOut.Mono,
                global::Android.Media.Encoding.Pcm16bit);
            if (minBuf <= 0) minBuf = 2048;

            // 使用新的 AudioAttributes 避免废弃警告
            var attributes = new global::Android.Media.AudioAttributes.Builder()
                .SetUsage(global::Android.Media.AudioUsageKind.Media)
                .SetContentType(global::Android.Media.AudioContentType.Music)
                .Build();

            _track = new global::Android.Media.AudioTrack.Builder()
                .SetAudioAttributes(attributes)
                .SetAudioFormat(new global::Android.Media.AudioFormat.Builder()
                    .SetEncoding(global::Android.Media.Encoding.Pcm16bit)
                    .SetSampleRate(8000)
                    .SetChannelMask(global::Android.Media.ChannelOut.Mono)
                    .Build())
                .SetBufferSizeInBytes(minBuf)
                .SetTransferMode(global::Android.Media.AudioTrackMode.Stream)
                .Build();

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
