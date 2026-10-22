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

            // 修复警告：增加 null 检查
            var attributesBuilder = new global::Android.Media.AudioAttributes.Builder();
            if (attributesBuilder != null)
            {
                attributesBuilder.SetUsage(global::Android.Media.AudioUsageKind.Media);
                attributesBuilder.SetContentType(global::Android.Media.AudioContentType.Music);
            }
            var attributes = attributesBuilder?.Build();

            var formatBuilder = new global::Android.Media.AudioFormat.Builder();
            if (formatBuilder != null)
            {
                formatBuilder.SetEncoding(global::Android.Media.Encoding.Pcm16bit);
                formatBuilder.SetSampleRate(8000);
                formatBuilder.SetChannelMask(global::Android.Media.ChannelOut.Mono);
            }
            var format = formatBuilder?.Build();

            var trackBuilder = new global::Android.Media.AudioTrack.Builder();
            if (attributes != null) trackBuilder.SetAudioAttributes(attributes);
            if (format != null) trackBuilder.SetAudioFormat(format);
            
            trackBuilder.SetBufferSizeInBytes(minBuf);
            trackBuilder.SetTransferMode(global::Android.Media.AudioTrackMode.Stream);
            
            _track = trackBuilder.Build();
            if (_track == null) return;

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
