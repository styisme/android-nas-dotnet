using System.Net;
using System.Net.Sockets;
using System.Text;
using AndroidNas.Core;

namespace AndroidNas.Server.Protocols;

public sealed class FtpServer : IDisposable
{
    private readonly int _port;
    private readonly string _rootDir;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }

    public FtpServer(int port, string rootDir)
    {
        _port = port;
        _rootDir = rootDir;
    }

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Start();
        IsRunning = true;
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        NasLog.Add("127.0.0.1", "FTP 已启动: " + _port);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(ct); }
            catch { break; }

            _ = Task.Run(() => new FtpSession(client, _rootDir).RunAsync(ct));
        }
    }

    public void Dispose()
    {
        if (!IsRunning) return;
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _listener = null;
        _cts?.Dispose();
        _cts = null;
        IsRunning = false;
        NasLog.Add("127.0.0.1", "FTP 已停止");
    }

    private sealed class FtpSession
    {
        private readonly TcpClient _client;
        private readonly string _rootDir;
        private readonly string _rootFull;
        private NetworkStream _stream = null!;
        private StreamReader _reader = null!;
        private StreamWriter _writer = null!;

        private string _currentDir;
        private string _user = "";
        private TcpListener? _dataListener;
        private TcpClient? _dataClient;
        private string? _renameFrom;

        public FtpSession(TcpClient client, string rootDir)
        {
            _client = client;
            _rootDir = rootDir;
            _rootFull = Path.GetFullPath(rootDir).TrimEnd('/');
            _currentDir = _rootFull;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, new UTF8Encoding(false));
                _writer = new StreamWriter(_stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };

                await Reply("220 Android NAS Next FTP Server Ready");

                string? line;
                while ((line = await _reader.ReadLineAsync()) != null)
                {
                    if (ct.IsCancellationRequested) break;
                    var sp = line.Split(' ', 2);
                    var cmd = sp[0].ToUpperInvariant();
                    var arg = sp.Length > 1 ? sp[1] : "";
                    await Dispatch(cmd, arg);
                }
            }
            catch { }
            finally
            {
                CloseData();
                try { _client.Close(); } catch { }
            }
        }

        private async Task Dispatch(string cmd, string arg)
        {
            switch (cmd)
            {
                case "USER":
                    _user = arg;
                    await Reply("331 Password required");
                    break;

                case "PASS":
                    {
                        var enc = Convert.ToBase64String(Encoding.UTF8.GetBytes(_user + ":" + arg));
                        if (AuthService.VerifyRole("Basic " + enc) > 0)
                            await Reply("230 Login successful");
                        else
                            await Reply("530 Login incorrect");
                        break;
                    }

                case "SYST":  await Reply("215 UNIX Type: L8"); break;
                case "FEAT":  await Reply("211-Features:\r\n SIZE\r\n MDTM\r\n UTF8\r\n211 End"); break;
                case "OPTS":  await Reply("200 OK"); break;
                case "TYPE":  await Reply("200 Type set to " + arg); break;

                case "PWD":
                case "XPWD":  await Reply("257 \"" + VirtualPath(_currentDir) + "\" is current directory"); break;

                case "CWD":
                    {
                        var t = SafeDir(arg);
                        if (t != null) { _currentDir = t; await Reply("250 Directory changed"); }
                        else await Reply("550 Directory not found");
                        break;
                    }

                case "CDUP":
                    {
                        var p = Path.GetDirectoryName(_currentDir);
                        if (p != null && p.StartsWith(_rootFull, StringComparison.Ordinal))
                        { _currentDir = p; await Reply("250 OK"); }
                        else await Reply("550 Cannot go up");
                        break;
                    }

                case "PASV":  await EnterPassive(); break;
                case "LIST":
                case "NLST":  await SendList(arg); break;
                case "RETR":  await SendFile(arg); break;
                case "STOR":  await ReceiveFile(arg); break;
                case "DELE":  await DeleteFile(arg); break;

                case "MKD":
                case "XMKD":
                    {
                        var f = SafePath(arg);
                        if (f != null && !Directory.Exists(f) && !File.Exists(f))
                        {
                            Directory.CreateDirectory(f);
                            await Reply("257 \"" + VirtualPath(f) + "\" created");
                        }
                        else await Reply("550 Create failed");
                        break;
                    }

                case "RMD":
                case "XRMD":
                    {
                        var f = SafePath(arg);
                        if (f != null && Directory.Exists(f))
                        {
                            try { Directory.Delete(f, false); await Reply("250 Removed"); }
                            catch { await Reply("550 Directory not empty"); }
                        }
                        else await Reply("550 Remove failed");
                        break;
                    }

                case "RNFR":
                    {
                        var f = SafePath(arg);
                        if (f != null && (File.Exists(f) || Directory.Exists(f)))
                        { _renameFrom = f; await Reply("350 Ready for RNTO"); }
                        else await Reply("550 Not found");
                        break;
                    }

                case "RNTO":
                    {
                        if (_renameFrom == null) { await Reply("503 RNFR required"); break; }
                        var to = SafePath(arg);
                        if (to == null) { await Reply("550 Bad name"); _renameFrom = null; break; }
                        try
                        {
                            if (File.Exists(_renameFrom))
                            {
                                if (File.Exists(to)) File.Delete(to);
                                File.Move(_renameFrom, to);
                            }
                            else if (Directory.Exists(_renameFrom))
                                Directory.Move(_renameFrom, to);
                            await Reply("250 Renamed");
                        }
                        catch { await Reply("550 Rename failed"); }
                        _renameFrom = null;
                        break;
                    }

                case "SIZE":
                    {
                        var f = SafePath(arg);
                        if (f != null && File.Exists(f)) await Reply("213 " + new FileInfo(f).Length);
                        else await Reply("550 Not found");
                        break;
                    }

                case "NOOP":  await Reply("200 OK"); break;
                case "QUIT":  await Reply("221 Goodbye"); try { _client.Close(); } catch { } break;
                default:      await Reply("502 Command not implemented: " + cmd); break;
            }
        }

        private async Task Reply(string msg) => await _writer.WriteLineAsync(msg);

        private async Task EnterPassive()
        {
            CloseData();
            try
            {
                _dataListener = new TcpListener(IPAddress.Any, 0);
                _dataListener.Start();
                var port = ((IPEndPoint)_dataListener.LocalEndpoint).Port;
                var ip = NetUtils.GetLocalIp().Replace('.', ',');
                await Reply("227 Entering Passive Mode (" + ip + "," + ((port >> 8) & 0xFF) + "," + (port & 0xFF) + ")");
            }
            catch { await Reply("425 Can't open data connection"); }
        }

        private async Task<TcpClient?> WaitData()
        {
            if (_dataListener == null) return null;
            try
            {
                _dataClient = await _dataListener.AcceptTcpClientAsync();
                return _dataClient;
            }
            catch { return null; }
        }

        private void CloseData()
        {
            try { _dataClient?.Close(); } catch { }
            try { _dataListener?.Stop(); } catch { }
            _dataClient = null;
            _dataListener = null;
        }

        private async Task SendList(string arg)
        {
            try
            {
                var dc = await WaitData();
                if (dc == null) { await Reply("425 Data connection failed"); return; }

                using var dw = new StreamWriter(dc.GetStream(), new UTF8Encoding(false))
                    { AutoFlush = true, NewLine = "\r\n" };

                var dir = _currentDir;
                if (!string.IsNullOrWhiteSpace(arg) && !arg.StartsWith('-'))
                {
                    var t = SafePath(arg);
                    if (t != null && Directory.Exists(t)) dir = t;
                }

                foreach (var f in Directory.EnumerateFileSystemEntries(dir))
                {
                    var isDir = Directory.Exists(f);
                    var size = isDir ? 0 : new FileInfo(f).Length;
                    var perms = isDir ? "drwxr-xr-x" : "-rw-r--r--";
                    var ts = DateTime.Now.ToString("MMM dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    await dw.WriteLineAsync(perms + " 1 ftp ftp " + size + " " + ts + " " + Path.GetFileName(f));
                }
                await dw.FlushAsync();
                dw.Close();
                CloseData();
                await Reply("226 Transfer complete");
            }
            catch { await Reply("425 Data connection failed"); CloseData(); }
        }

        private async Task SendFile(string filename)
        {
            var f = SafePath(filename);
            if (f == null || !File.Exists(f)) { await Reply("550 File not found"); return; }
            try
            {
                var dc = await WaitData();
                if (dc == null) { await Reply("425 Data connection failed"); return; }
                await Reply("150 Opening data connection");

                await using var fs = File.OpenRead(f);
                await using var ds = dc.GetStream();
                await fs.CopyToAsync(ds);
                await ds.FlushAsync();
                CloseData();
                await Reply("226 Transfer complete");
            }
            catch { await Reply("426 Transfer aborted"); CloseData(); }
        }

        private async Task ReceiveFile(string filename)
        {
            var f = SafePath(filename);
            if (f == null) { await Reply("550 Access denied"); return; }
            try
            {
                var parent = Path.GetDirectoryName(f);
                if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

                var dc = await WaitData();
                if (dc == null) { await Reply("425 Data connection failed"); return; }
                await Reply("150 Opening data connection");

                await using var fs = File.Create(f);
                await using var ds = dc.GetStream();
                await ds.CopyToAsync(fs);
                await fs.FlushAsync();
                CloseData();
                await Reply("226 Transfer complete");
            }
            catch
            {
                try { if (File.Exists(f)) File.Delete(f); } catch { }
                await Reply("426 Transfer aborted");
                CloseData();
            }
        }

        private async Task DeleteFile(string filename)
        {
            var f = SafePath(filename);
            if (f != null && File.Exists(f))
            {
                try { File.Delete(f); await Reply("250 File deleted"); }
                catch { await Reply("550 Delete failed"); }
            }
            else await Reply("550 Delete failed");
        }

        private string? SafeDir(string arg)
        {
            var p = SafePath(arg);
            return p != null && Directory.Exists(p) ? p : null;
        }

        private string? SafePath(string arg)
        {
            try
            {
                if (arg == null) return null;
                arg = arg.Replace('\\', '/');

                string combined;
                if (arg.StartsWith("/"))
                    combined = Path.GetFullPath(Path.Combine(_rootFull, arg.TrimStart('/')));
                else
                    combined = Path.GetFullPath(Path.Combine(_currentDir, arg));

                if (!combined.Equals(_rootFull, StringComparison.Ordinal) &&
                    !combined.StartsWith(_rootFull + "/", StringComparison.Ordinal))
                    return null;
                return combined;
            }
            catch { return null; }
        }

        private string VirtualPath(string physical)
        {
            if (physical.Equals(_rootFull, StringComparison.Ordinal)) return "/";
            if (physical.StartsWith(_rootFull, StringComparison.Ordinal))
                return physical.Substring(_rootFull.Length).Replace('\\', '/');
            return "/";
        }
    }
}
