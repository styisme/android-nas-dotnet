using System.Text.RegularExpressions;
using AndroidNas.Core;
using Renci.SshNet;

namespace AndroidNas.Server.Tunneling;

public static class FreeTunnelManager
{
    private sealed class Node
    {
        public string Name { get; }
        public string Host { get; }
        public int Port { get; }
        public string User { get; }
        public string Regex { get; }
        public Node(string name, string host, int port, string user, string regex)
        {
            Name = name; Host = host; Port = port; User = user; Regex = regex;
        }
    }

    private static readonly Node[] Nodes =
    {
        new Node("Tunnl.gg",     "proxy.tunnl.gg",    22, "tunnl",     @"https://[a-zA-Z0-9.\-]+\.tunnl\.gg"),
        new Node("Skytunnel",    "skytunnel.pp.ua",   22, "skytunnel", @"https://[a-zA-Z0-9.\-]+\.skytunnel\.pp\.ua"),
        new Node("Serveo",       "serveo.net",        22, "serveo",    @"https://[a-zA-Z0-9.\-]+\.serveo\.net"),
        new Node("Localhost.run","localhost.run",     22, "nokey",     @"https://[a-zA-Z0-9.\-]+\.life"),
        new Node("Pinggy",       "a.pinggy.io",       22, "pinggy",    @"https://[a-zA-Z0-9.\-]+\.pinggy\.link"),
        new Node("Tuns.sh",      "tuns.sh",           22, "tuns",      @"https://[a-zA-Z0-9.\-]+\.tuns\.sh"),
        new Node("Localto.net",  "localto.net",       22, "localto",   @"https://[a-zA-Z0-9.\-]+\.localto\.net")
    };

    private static SshClient? _client;
    private static ShellStream? _shell;
    private static CancellationTokenSource? _cts;
    private static readonly object _lock = new();

    public static bool IsRunning { get; private set; }
    public static string PublicUrl { get; private set; } = "";
    public static string StatusMessage { get; private set; } = "";

    public static string[] GetNodeNames()
    {
        var names = new string[Nodes.Length + 1];
        names[0] = "自动选择 (推荐)";
        for (var i = 0; i < Nodes.Length; i++) names[i + 1] = Nodes[i].Name;
        return names;
    }

    public static void Start(int localPort, int nodeIndex)
    {
        if (IsRunning) return;
        IsRunning = true;
        PublicUrl = "";
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        Task.Run(() =>
        {
            if (nodeIndex == 0)
            {
                foreach (var node in Nodes)
                {
                    if (ct.IsCancellationRequested) break;
                    if (TryConnect(node, localPort, ct)) break;
                }
            }
            else if (nodeIndex > 0 && nodeIndex <= Nodes.Length)
            {
                TryConnect(Nodes[nodeIndex - 1], localPort, ct);
            }

            if (string.IsNullOrEmpty(PublicUrl))
            {
                StatusMessage = "所有节点均失败";
                IsRunning = false;
            }
        });
    }

    private static bool TryConnect(Node node, int localPort, CancellationToken ct)
    {
        try
        {
            var info = new ConnectionInfo(node.Host, node.Port, node.User,
                new PasswordAuthenticationMethod(node.User, ""))
            {
                Timeout = TimeSpan.FromSeconds(8)
            };

            var client = new SshClient(info);
            client.Connect();
            if (!client.IsConnected) return false;

            client.AddForwardedPort(new ForwardedPortRemote(
                "0.0.0.0", 80, "127.0.0.1", (uint)localPort));

            var shell = client.CreateShellStream("xterm", 80, 24, 800, 600, 4096);
            if (shell == null) { client.Disconnect(); return false; }

            shell.WriteLine("ssh -R 80:localhost:" + localPort + " " + node.User + "@" + node.Host);
            Thread.Sleep(2000);

            StatusMessage = "正在尝试 " + node.Name + "…";
            NasLog.Add("TUNNEL", StatusMessage);

            var pattern = new Regex(node.Regex, RegexOptions.IgnoreCase);
            for (var i = 0; i < 20 && !ct.IsCancellationRequested; i++)
            {
                Thread.Sleep(500);
                try
                {
                    var output = shell.Read();
                    if (string.IsNullOrEmpty(output)) continue;
                    var m = pattern.Match(output);
                    if (m.Success)
                    {
                        PublicUrl = m.Value;
                        lock (_lock) { _client = client; _shell = shell; }
                        StatusMessage = node.Name + " 连接成功";
                        NasLog.Add("TUNNEL", node.Name + " → " + PublicUrl);
                        return true;
                    }
                }
                catch { }
            }

            try { shell.Dispose(); } catch { }
            try { client.Disconnect(); } catch { }
            return false;
        }
        catch (Exception e)
        {
            StatusMessage = node.Name + " 失败: " + e.Message;
            NasLog.Add("TUNNEL", StatusMessage);
            return false;
        }
    }

    public static void Stop()
    {
        IsRunning = false;
        PublicUrl = "";
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        _cts = null;

        lock (_lock)
        {
            try { _shell?.Dispose(); } catch { }
            _shell = null;
            try { _client?.Disconnect(); } catch { }
            try { _client?.Dispose(); } catch { }
            _client = null;
        }
        NasLog.Add("TUNNEL", "免费穿透已停止");
    }
}
