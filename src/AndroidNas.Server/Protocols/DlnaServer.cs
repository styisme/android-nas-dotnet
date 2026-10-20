using System.Net;
using System.Net.Sockets;
using System.Text;
using AndroidNas.Core;

namespace AndroidNas.Server.Protocols;

public sealed class DlnaServer : IDisposable
{
    private const string SsdpGroup = "239.255.255.250";
    private const int SsdpPort = 1900;

    private readonly int _webPort;
    private UdpClient? _udp;
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }

    public DlnaServer(int webPort) { _webPort = webPort; }

    public void Start()
    {
        if (IsRunning) return;
        try
        {
            _cts = new CancellationTokenSource();
            _udp = new UdpClient();
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, SsdpPort));
            _udp.JoinMulticastGroup(IPAddress.Parse(SsdpGroup));
            IsRunning = true;
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            NasLog.Add("127.0.0.1", "DLNA SSDP 已启动: 1900");
        }
        catch (Exception e)
        {
            NasLog.Add("127.0.0.1", "DLNA 启动失败: " + e.Message);
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
                if (msg.StartsWith("M-SEARCH", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondAsync(result.RemoteEndPoint, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    private async Task RespondAsync(IPEndPoint remote, CancellationToken ct)
    {
        if (_udp == null) return;
        try
        {
            var localIp = NetUtils.GetLocalIp();
            var response =
                "HTTP/1.1 200 OK\r\n" +
                "CACHE-CONTROL: max-age=1800\r\n" +
                "EXT:\r\n" +
                "LOCATION: http://" + localIp + ":" + _webPort + "/dlna/description.xml\r\n" +
                "SERVER: Android/1.0 UPnP/1.0 AndroidNAS/4.0\r\n" +
                "ST: urn:schemas-upnp-org:device:MediaServer:1\r\n" +
                "USN: uuid:" + AppSettings.Fingerprint + "::urn:schemas-upnp-org:device:MediaServer:1\r\n" +
                "\r\n";
            var bytes = Encoding.UTF8.GetBytes(response);
            await _udp.SendAsync(bytes, remote, ct);
            NasLog.Add(remote.Address.ToString(), "DLNA 响应 M-SEARCH");
        }
        catch { }
    }

    public void Dispose()
    {
        if (!IsRunning) return;
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _udp?.DropMulticastGroup(IPAddress.Parse(SsdpGroup)); } catch { }
        try { _udp?.Close(); } catch { }
        try { _udp?.Dispose(); } catch { }
        _udp = null;
        _cts?.Dispose();
        _cts = null;
        NasLog.Add("127.0.0.1", "DLNA SSDP 已停止");
    }
}
