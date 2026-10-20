using AndroidNas.Core;
using Renci.SshNet;

namespace AndroidNas.Server.Tunneling;

public static class SshTunnelManager
{
    private static SshClient? _client;
    private static ForwardedPortRemote? _port;
    private static readonly object _lock = new();

    public static bool IsRunning
    {
        get { lock (_lock) return _client != null && _client.IsConnected; }
    }

    public static string StatusMessage { get; private set; } = "";

    public static void Start(string host, int port, string user, string pass,
                             int remotePort, int localPort)
    {
        if (IsRunning) { StatusMessage = "已在连接中"; return; }

        Task.Run(() =>
        {
            try
            {
                var info = new ConnectionInfo(host, port, user,
                    new PasswordAuthenticationMethod(user, pass))
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                var client = new SshClient(info);
                // 修复警告：使用 KeepAliveInterval 代替手动 SendKeepAlive
                client.KeepAliveInterval = TimeSpan.FromSeconds(10);
                client.Connect();
                
                if (!client.IsConnected)
                {
                    StatusMessage = "SSH 连接失败";
                    return;
                }

                var forward = new ForwardedPortRemote(
                    "0.0.0.0", (uint)remotePort, "127.0.0.1", (uint)localPort);
                client.AddForwardedPort(forward);
                forward.Start();

                lock (_lock)
                {
                    _client = client;
                    _port = forward;
                }

                StatusMessage = "已连接 " + host + ":" + port + "  →  公网端口 " + remotePort;
                NasLog.Add("TUNNEL", StatusMessage);
            }
            catch (Exception e)
            {
                StatusMessage = "SSH 失败: " + e.Message;
                NasLog.Add("TUNNEL", StatusMessage);
                Stop();
            }
        });
    }

    public static void Stop()
    {
        lock (_lock)
        {
            try { _port?.Stop(); } catch { }
            try { _port?.Dispose(); } catch { }
            _port = null;
            try { _client?.Disconnect(); } catch { }
            try { _client?.Dispose(); } catch { }
            _client = null;
        }
        NasLog.Add("TUNNEL", "自定义 SSH 已断开");
    }
}
