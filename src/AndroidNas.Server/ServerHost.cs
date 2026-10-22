using System.Net;
using System.Text;
using System.Text.Json;
using System.Security;
using System.IO.Compression;
using AndroidNas.Core;
using AndroidNas.Server.Files;
using AndroidNas.Server.Protocols;
using AndroidNas.Server.Endpoints;

namespace AndroidNas.Server;

public sealed class ServerHost : IDisposable
{
    private HttpListener? _listener;
    private FtpServer? _ftp;
    private WebDavServer? _webdav;
    private DlnaServer? _dlna;
    private LocalSendServer? _localsend;
    private LocalSendDiscovery? _lsDiscovery;
    private BroadcastSyncer? _broadcast;
    private CancellationTokenSource? _cts;
    private string _clipboard = "";

    public void StartHttp(int port, bool https = false, int httpsPort = 8443)
    {
        try
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");
            if (https)
            {
                try { _listener.Prefixes.Add($"https://*:{httpsPort}/"); } catch { }
            }
            _listener.Start();
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            NasLog.Add("127.0.0.1", "Web 服务已就绪");
        }
        catch (Exception e)
        {
            // 捕获异常，防止崩溃整个 App
            NasLog.Add("SYSTEM", "Web 启动失败: " + e.Message);
            _listener = null;
        }

        try { if (AppSettings.FtpEnabled) StartFtp(AppSettings.FtpPort); } catch { }
        try { if (AppSettings.WebDavEnabled) StartWebDav(AppSettings.WebDavPort); } catch { }
        try { if (AppSettings.DlnaEnabled) StartDlna(port); } catch { }
        try { if (AppSettings.LocalSendEnabled) StartLocalSend(); } catch { }
        try { StartBroadcastSync(); } catch { }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(ctx));
            }
            catch { break; }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        var path = req.Url?.AbsolutePath ?? "/";

        try
        {
            if (!path.StartsWith("/share") && !AuthService.Verify(req.Headers["Authorization"]))
            {
                res.StatusCode = 401;
                res.AddHeader("WWW-Authenticate", "Basic realm=\"Android NAS\"");
                res.Close();
                return;
            }

            res.AddHeader("Access-Control-Allow-Origin", "*");

            if (path == "/" || path == "/index.html") await ServeEmbeddedIndexAsync(res);
            else if (path == "/api/status") await WriteJsonAsync(res, new { lowPower = AppSettings.LowPowerMode, clipboardEnabled = AppSettings.ClipboardEnabled, globalMode = AppSettings.GlobalMode });
            else if (path == "/api/clipboard")
            {
                if (req.HttpMethod == "GET") await WriteTextAsync(res, _clipboard);
                else { var form = await ParseFormAsync(req); _clipboard = form.GetValueOrDefault("content", ""); await WriteTextAsync(res, "OK"); }
            }
            else if (path == "/api/list") await HandleListAsync(req, res);
            else if (path == "/api/download") await HandleDownloadAsync(req, res);
            // 简化的路由分支
            else { res.StatusCode = 404; await WriteTextAsync(res, "Not Found"); }
        }
        catch (Exception e)
        {
            if (res.OutputStream.CanWrite) { res.StatusCode = 500; await WriteTextAsync(res, "Error: " + e.Message); }
        }
        finally { try { res.Close(); } catch { } }
    }

    private async Task WriteTextAsync(HttpListenerResponse res, string text, string type = "text/plain; charset=utf-8")
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        res.ContentType = type;
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
    }

    private async Task WriteJsonAsync<T>(HttpListenerResponse res, T obj) => await WriteTextAsync(res, JsonSerializer.Serialize(obj), "application/json; charset=utf-8");

    private async Task<Dictionary<string, string>> ParseFormAsync(HttpListenerRequest req)
    {
        var dict = new Dictionary<string, string>();
        if (!req.HasEntityBody) return dict;
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        if (req.ContentType?.Contains("application/x-www-form-urlencoded") == true)
            foreach (var pair in body.Split('&')) { var kv = pair.Split('='); if (kv.Length == 2) dict[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]); }
        return dict;
    }

    private IFileRootProvider _root = new PhysicalRootProvider(AppSettings.RootPath, AppSettings.GlobalMode);

    private async Task HandleListAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        try
        {
            var dir = _root.Resolve(req.QueryString["path"] ?? "/");
            if (!Directory.Exists(dir)) { res.StatusCode = 404; return; }
            var entries = new DirectoryInfo(dir).EnumerateFileSystemInfos().OrderBy(e => e is not DirectoryInfo).ThenBy(e => e.Name).Select(e => new {
                name = e.Name, dir = e is DirectoryInfo, size = e is FileInfo fi ? fi.Length : 0, isTxt = MediaTypeHelper.IsTxt(e.Name), isMedia = MediaTypeHelper.IsMedia(e.Name), isZip = MediaTypeHelper.IsZip(e.Name)
            }).ToArray();
            await WriteJsonAsync(res, new { path = _root.ToVirtual(dir), files = entries });
        }
        catch { res.StatusCode = 400; }
    }

    private async Task HandleDownloadAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        try
        {
            var full = _root.Resolve(req.QueryString["path"] ?? "");
            if (!File.Exists(full)) { res.StatusCode = 404; return; }
            var fi = new FileInfo(full);
            var range = req.Headers["Range"];
            res.AddHeader("Accept-Ranges", "bytes");
            res.ContentType = "application/octet-stream";
            res.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(full)}\"");
            if (range != null && range.StartsWith("bytes="))
            {
                var parts = range.Substring(6).Split('-');
                long start = long.Parse(parts[0]); long end = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? long.Parse(parts[1]) : fi.Length - 1;
                if (end >= fi.Length) end = fi.Length - 1;
                long length = end - start + 1;
                res.StatusCode = 206; res.AddHeader("Content-Range", $"bytes {start}-{end}/{fi.Length}"); res.ContentLength64 = length;
                using var fs = File.OpenRead(full); fs.Seek(start, SeekOrigin.Begin);
                var buf = new byte[81920];
                while (length > 0) { int toRead = (int)Math.Min(buf.Length, length); int read = await fs.ReadAsync(buf, 0, toRead); if (read == 0) break; await res.OutputStream.WriteAsync(buf, 0, read); length -= read; }
            }
            else { res.StatusCode = 200; res.ContentLength64 = fi.Length; using var fs = File.OpenRead(full); await fs.CopyToAsync(res.OutputStream); }
        }
        catch { res.StatusCode = 400; }
    }

    private async Task ServeEmbeddedIndexAsync(HttpListenerResponse res)
    {
        var stream = typeof(ServerHost).Assembly.GetManifestResourceStream("AndroidNas.Server.Resources.wwwroot.index.html");
        if (stream == null) { await WriteTextAsync(res, "UI not found"); return; }
        res.ContentType = "text/html; charset=utf-8"; res.ContentLength64 = stream.Length; await stream.CopyToAsync(res.OutputStream);
    }

    public void StartFtp(int port) { if (_ftp != null) return; try { _ftp = new FtpServer(port, AppSettings.RootPath); _ftp.Start(); } catch { } }
    public void StartWebDav(int port) { if (_webdav != null) return; try { _webdav = new WebDavServer(port, AppSettings.RootPath); _webdav.Start(); } catch { } }
    public void StartDlna(int webPort) { if (_dlna != null) return; try { _dlna = new DlnaServer(webPort); _dlna.Start(); } catch { } }
    public void StartLocalSend() { if (_localsend != null) return; try { _localsend = new LocalSendServer(); _localsend.Start(); _lsDiscovery = new LocalSendDiscovery(); _lsDiscovery.Start(); } catch { } }
    public void StartBroadcastSync() { if (_broadcast != null) return; try { _broadcast = new BroadcastSyncer(); _broadcast.Start(); } catch { } }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        try { _broadcast?.Dispose(); } catch { }
        try { _lsDiscovery?.Dispose(); } catch { }
        try { _localsend?.Dispose(); } catch { }
        try { _dlna?.Dispose(); } catch { }
        try { _ftp?.Dispose(); } catch { }
        try { _webdav?.Dispose(); } catch { }
    }
}
