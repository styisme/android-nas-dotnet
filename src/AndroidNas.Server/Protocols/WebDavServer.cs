using System.Net;
using System.Net.Sockets;
using System.Text;
using AndroidNas.Core;

namespace AndroidNas.Server.Protocols;

public sealed class WebDavServer : IDisposable
{
    private readonly int _port;
    private readonly string _rootDir;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }

    public WebDavServer(int port, string rootDir)
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
        NasLog.Add("127.0.0.1", "WebDAV 已启动: " + _port);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(ct); }
            catch { break; }
            _ = Task.Run(() => new ClientHandler(client, _rootDir).RunAsync(ct));
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
        NasLog.Add("127.0.0.1", "WebDAV 已停止");
    }

    private sealed class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly string _rootFull;
        private NetworkStream _stream = null!;

        public ClientHandler(TcpClient client, string rootDir)
        {
            _client = client;
            _rootFull = Path.GetFullPath(rootDir).TrimEnd('/');
        }

        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                _stream = _client.GetStream();

                var reader = new StreamReader(_stream, Encoding.UTF8, false, 8192, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestLine)) return;

                var sp = requestLine.Split(' ');
                if (sp.Length < 2) return;

                var method = sp[0].ToUpperInvariant();
                var uri = Uri.UnescapeDataString(sp[1]);
                if (uri.Contains('?')) uri = uri.Substring(0, uri.IndexOf('?'));
                if (!uri.StartsWith("/")) uri = "/" + uri;

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                long contentLength = 0;
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    var idx = line.IndexOf(':');
                    if (idx <= 0) continue;
                    var k = line.Substring(0, idx).Trim();
                    var v = line.Substring(idx + 1).Trim();
                    headers[k] = v;
                    if (k.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        long.TryParse(v, out contentLength);
                }

                if (method != "OPTIONS")
                {
                    headers.TryGetValue("Authorization", out var auth);
                    if (!AuthService.Verify(auth))
                    {
                        await WriteResponse(401, "Unauthorized",
                            new Dictionary<string, string>
                            {
                                ["WWW-Authenticate"] = "Basic realm=\"Android NAS Next WebDAV\"",
                                ["Content-Length"] = "0"
                            }, null);
                        return;
                    }
                }

                var target = SafePath(uri);
                if (target == null)
                {
                    await WriteResponse(403, "Forbidden",
                        new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                    return;
                }

                headers.TryGetValue("Depth", out var depth);
                if (depth == null) depth = "1";

                switch (method)
                {
                    case "OPTIONS":
                        await WriteResponse(200, "OK", new Dictionary<string, string>
                        {
                            ["DAV"] = "1,2",
                            ["Allow"] = "OPTIONS,GET,HEAD,PUT,DELETE,PROPFIND,PROPPATCH,MKCOL,COPY,MOVE",
                            ["Content-Length"] = "0"
                        }, null);
                        break;

                    case "PROPFIND":
                        await HandlePropfind(target, uri, depth);
                        break;

                    case "GET":
                    case "HEAD":
                        await HandleGet(target, method == "HEAD");
                        break;

                    case "PUT":
                        await HandlePut(target, headers);
                        break;

                    case "MKCOL":
                        if (Directory.Exists(target) || File.Exists(target))
                        {
                            await WriteResponse(405, "Method Not Allowed",
                                new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                        }
                        else
                        {
                            Directory.CreateDirectory(target);
                            await WriteResponse(201, "Created",
                                new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                        }
                        break;

                    case "DELETE":
                        if (File.Exists(target))
                        {
                            File.Delete(target);
                            await WriteResponse(204, "No Content",
                                new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                        }
                        else if (Directory.Exists(target))
                        {
                            try
                            {
                                Directory.Delete(target, true);
                                await WriteResponse(204, "No Content",
                                    new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                            }
                            catch
                            {
                                await WriteResponse(403, "Forbidden",
                                    new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                            }
                        }
                        else
                        {
                            await WriteResponse(404, "Not Found",
                                new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                        }
                        break;

                    default:
                        await WriteResponse(405, "Method Not Allowed",
                            new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                        break;
                }
            }
            catch { }
            finally
            {
                try { _client.Close(); } catch { }
            }
        }

        private async Task HandlePropfind(string target, string uri, string depth)
        {
            if (!File.Exists(target) && !Directory.Exists(target))
            {
                await WriteResponse(404, "Not Found",
                    new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n");
            sb.Append("<D:multistatus xmlns:D=\"DAV:\">\r\n");

            AppendProp(sb, target, uri);

            if (depth != "0" && Directory.Exists(target))
            {
                foreach (var child in Directory.EnumerateFileSystemEntries(target))
                {
                    var childName = Path.GetFileName(child);
                    var childUri = uri.TrimEnd('/') + "/" + Uri.EscapeDataString(childName);
                    AppendProp(sb, child, childUri);
                }
            }

            sb.Append("</D:multistatus>");
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            await WriteRawResponse(207, "Multi-Status", new Dictionary<string, string>
            {
                ["Content-Type"] = "application/xml; charset=\"utf-8\"",
                ["Content-Length"] = bytes.Length.ToString()
            }, bytes);
        }

        private void AppendProp(StringBuilder sb, string path, string uri)
        {
            var isDir = Directory.Exists(path);
            var isFile = File.Exists(path);
            var length = isFile ? new FileInfo(path).Length : 0;
            var modified = isFile
                ? File.GetLastWriteTimeUtc(path)
                : Directory.GetLastWriteTimeUtc(path);

            sb.Append("<D:response>\r\n");
            sb.Append("<D:href>").Append(System.Security.SecurityElement.Escape(uri)).Append("</D:href>\r\n");
            sb.Append("<D:propstat>\r\n<D:prop>\r\n");
            sb.Append("<D:displayname>").Append(System.Security.SecurityElement.Escape(Path.GetFileName(path))).Append("</D:displayname>\r\n");
            if (isFile)
                sb.Append("<D:getcontentlength>").Append(length).Append("</D:getcontentlength>\r\n");
            sb.Append("<D:getlastmodified>").Append(modified.ToString("R")).Append("</D:getlastmodified>\r\n");
            sb.Append("<D:resourcetype>");
            if (isDir) sb.Append("<D:collection/>");
            sb.Append("</D:resourcetype>\r\n");
            sb.Append("</D:prop>\r\n<D:status>HTTP/1.1 200 OK</D:status>\r\n</D:propstat>\r\n</D:response>\r\n");
        }

        private async Task HandleGet(string target, bool headOnly)
        {
            if (!File.Exists(target))
            {
                await WriteResponse(404, "Not Found",
                    new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
                return;
            }

            var bytes = await File.ReadAllBytesAsync(target);
            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/octet-stream",
                ["Content-Length"] = bytes.Length.ToString()
            };
            await WriteRawResponse(200, "OK", headers, headOnly ? Array.Empty<byte>() : bytes);
        }

        private async Task HandlePut(string target, Dictionary<string, string> headers)
        {
            try
            {
                var parent = Path.GetDirectoryName(target);
                if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

                var existed = File.Exists(target);
                await using (var fs = File.Create(target))
                {
                    var buf = new byte[81920];
                    int n;
                    while ((n = await _stream.ReadAsync(buf)) > 0)
                        await fs.WriteAsync(buf.AsMemory(0, n));
                }
                await WriteResponse(existed ? 204 : 201, existed ? "No Content" : "Created",
                    new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
            }
            catch
            {
                await WriteResponse(500, "Internal Server Error",
                    new Dictionary<string, string> { ["Content-Length"] = "0" }, null);
            }
        }

        private string? SafePath(string uri)
        {
            try
            {
                var combined = Path.GetFullPath(Path.Combine(_rootFull, uri.TrimStart('/')));
                if (!combined.Equals(_rootFull, StringComparison.Ordinal) &&
                    !combined.StartsWith(_rootFull + "/", StringComparison.Ordinal))
                    return null;
                return combined;
            }
            catch { return null; }
        }

        private Task WriteResponse(int code, string reason,
            Dictionary<string, string> headers, byte[]? body)
            => WriteRawResponse(code, reason, headers, body ?? Array.Empty<byte>());

        private async Task WriteRawResponse(int code, string reason,
            Dictionary<string, string> headers, byte[] body)
        {
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(code).Append(" ").Append(reason).Append("\r\n");
            foreach (var kv in headers) sb.Append(kv.Key).Append(": ").Append(kv.Value).Append("\r\n");
            sb.Append("Connection: close\r\n");
            sb.Append("\r\n");

            var head = Encoding.UTF8.GetBytes(sb.ToString());
            await _stream.WriteAsync(head);
            if (body.Length > 0) await _stream.WriteAsync(body);
            await _stream.FlushAsync();
        }
    }
}
