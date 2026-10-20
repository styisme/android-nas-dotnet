using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AndroidNas.Core;

namespace AndroidNas.Server.Protocols;

public sealed class LocalSendDiscovery : IDisposable
{
    private const string Group = "224.0.0-preview.167";
    private const int Port = 53317;

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
            _udp.JoinMulticastGroup(IPAddress.Parse(Group));
            IsRunning = true;
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            NasLog.Add("127.0.0.1", "LocalSend 发现已启动: " + Group + ":" + Port);
        }
        catch (Exception e)
        {
            NasLog.Add("127.0.0.1", "LocalSend 发现启动失败: " + e.Message);
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
                var msg = Encoding.UTF8.GetString(result.Buffer);

                if (!msg.Contains("\"fingerprint\"")) continue;
                if (msg.Contains(AppSettings.Fingerprint)) continue;

                var info = new
                {
                    alias = AppSettings.DeviceAlias,
                    version = "2.2",
                    deviceModel = "Android",
                    deviceType = "desktop",
                    fingerprint = AppSettings.Fingerprint,
                    port = Port,
                    protocol = "http",
                    download = false
                };
                var json = JsonSerializer.Serialize(info);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _udp.SendAsync(bytes, result.RemoteEndPoint, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    public void Dispose()
    {
        if (!IsRunning) return;
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _udp?.DropMulticastGroup(IPAddress.Parse(Group)); } catch { }
        try { _udp?.Close(); } catch { }
        try { _udp?.Dispose(); } catch { }
        _udp = null;
        _cts?.Dispose();
        _cts = null;
        NasLog.Add("127.0.0.1", "LocalSend 发现已停止");
    }
}
