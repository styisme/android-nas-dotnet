using System.Net;
using System.Net.Sockets;
using System.Text;
using AndroidNas.Core;

namespace AndroidNas.Server.Protocols;

public sealed class BroadcastSyncer : IDisposable
{
    public const int Port = 8090;

    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        try
        {
            _cts = new CancellationTokenSource();
            _udp = new UdpClient();
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
            _udp.EnableBroadcast = true;
            IsRunning = true;
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            NasLog.Add("127.0.0.1", "UDP 8090 协同已启动");
        }
        catch (Exception e)
        {
            NasLog.Add("127.0.0.1", "UDP 8090 启动失败: " + e.Message);
            Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udp != null)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct);
                var msg = Encoding.UTF8.GetString(result.Buffer).Trim();

                if (msg == "DISCOVER_TV")
                {
                    var reply = Encoding.UTF8.GetBytes("TV_HERE");
                    await _udp.SendAsync(reply, result.RemoteEndPoint, ct);
                    NasLog.Add(result.RemoteEndPoint.Address.ToString(), "收到 DISCOVER_TV，已回复 TV_HERE");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    public async Task BroadcastDiscoverAsync()
    {
        if (_udp == null) return;
        try
        {
            var bytes = Encoding.UTF8.GetBytes("DISCOVER_TV");
            await _udp.SendAsync(bytes, new IPEndPoint(IPAddress.Broadcast, Port));
        }
        catch { }
    }

    public void Dispose()
    {
        if (!IsRunning) return;
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _udp?.Close(); } catch { }
        try { _udp?.Dispose(); } catch { }
        _udp = null;
        _cts?.Dispose();
        _cts = null;
        NasLog.Add("127.0.0.1", "UDP 8090 协同已停止");
    }
}
