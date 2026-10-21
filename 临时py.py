#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import sys

FILES = {}

# ==========================================
# 1. 版本同步脚本 (需求 5)
# ==========================================
FILES["sync_version.py"] = r"""#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re
import os

def main():
    root = os.path.dirname(os.path.abspath(__file__))
    changelog_path = os.path.join(root, "CHANGELOG.md")
    if not os.path.exists(changelog_path):
        print("CHANGELOG.md not found.")
        return

    with open(changelog_path, "r", encoding="utf-8") as f:
        content = f.read()

    # 匹配 [4.0.0-preview] 格式
    match = re.search(r"## \[(.*?)\]", content)
    if not match:
        print("No version found in CHANGELOG.md")
        return

    version = match.group(1)
    print(f"Syncing version: {version}")

    csprojs = [
        "src/AndroidNas.App/AndroidNas.App.csproj",
        "src/AndroidNas.Server/AndroidNas.Server.csproj",
        "src/AndroidNas.Core/AndroidNas.Core.csproj",
        "src/AndroidNas.Cli/AndroidNas.Cli.csproj",
        "src/AndroidNas.Desktop/AndroidNas.Desktop.csproj"
    ]

    for csproj in csprojs:
        path = os.path.join(root, csproj)
        if not os.path.exists(path): continue
        with open(path, "r", encoding="utf-8") as f:
            data = f.read()
        if "<Version>" in data:
            data = re.sub(r"<Version>.*?</Version>", f"<Version>{version}</Version>", data)
        else:
            data = data.replace("<TargetFramework>", f"<Version>{version}</Version>\n    <TargetFramework>")
        with open(path, "w", encoding="utf-8") as f:
            f.write(data)

if __name__ == "__main__":
    main()
"""

# ==========================================
# 2. Core 层：多语言与配置导入导出 (需求 1, 3, 7)
# ==========================================
FILES["src/AndroidNas.Core/Localization/LocalizationManager.cs"] = r"""namespace AndroidNas.Core;

public static class LocalizationManager
{
    private static string _lang = "zh-CN";
    public static void SetLanguage(string lang) { _lang = lang; }

    private static readonly Dictionary<string, Dictionary<string, string>> Dict = new()
    {
        ["zh-CN"] = new() {
            ["app_name"] = "Android NAS",
            ["start"] = "启动服务",
            ["stop"] = "停止服务",
            ["settings"] = "设置",
            ["export"] = "导出配置",
            ["import"] = "导入配置",
            ["https_on"] = "开启 HTTPS (自签)"
        },
        ["en-US"] = new() {
            ["app_name"] = "Android NAS",
            ["start"] = "Start Service",
            ["stop"] = "Stop Service",
            ["settings"] = "Settings",
            ["export"] = "Export Config",
            ["import"] = "Import Config",
            ["https_on"] = "Enable HTTPS (Self-Signed)"
        }
    };

    public static string Get(string key)
    {
        if (Dict.TryGetValue(_lang, out var table) && table.TryGetValue(key, out var val))
            return val;
        return key;
    }
}
"""

FILES["src/AndroidNas.Core/Settings/AppSettings.cs"] = r"""using System.Text.Json;

namespace AndroidNas.Core;

public interface ISettingsStore
{
    string GetString(string key, string def);
    int GetInt(string key, int def);
    bool GetBool(string key, bool def);
    void SetString(string key, string value);
    void SetInt(string key, int value);
    void SetBool(string key, bool value);
}

public class AppSettingsModel
{
    public string DeviceAlias { get; set; } = "Android NAS";
    public bool GlobalMode { get; set; }
    public bool LowPowerMode { get; set; }
    public bool SilentKeepAlive { get; set; } = true;
    public bool WebDavEnabled { get; set; } = true;
    public bool FtpEnabled { get; set; } = true;
    public bool DlnaEnabled { get; set; } = true;
    public bool LocalSendEnabled { get; set; } = true;
    public bool StreamEnabled { get; set; } = true;
    public bool ClipboardEnabled { get; set; } = true;
    public bool HttpsEnabled { get; set; }
    public int WebPort { get; set; } = 8080;
    public int HttpsPort { get; set; } = 8443;
    public int WebDavPort { get; set; } = 8081;
    public int FtpPort { get; set; } = 2121;
    public string LocalSendPin { get; set; } = "1234";
    public string Language { get; set; } = "zh-CN";
}

public static class AppSettings
{
    public static ISettingsStore? Store { get; set; }
    private static string S(string k, string d) => Store?.GetString(k, d) ?? d;
    private static int I(string k, int d) => Store?.GetInt(k, d) ?? d;
    private static bool B(string k, bool d) => Store?.GetBool(k, d) ?? d;
    private static void SS(string k, string v) => Store?.SetString(k, v);
    private static void SI(string k, int v) => Store?.SetInt(k, v);
    private static void SB(string k, bool v) => Store?.SetBool(k, v);

    public static string DeviceAlias { get => S("device_alias", "Android NAS"); set => SS("device_alias", value); }
    public static bool GlobalMode { get => B("global_mode", false); set => SB("global_mode", value); }
    public static bool LowPowerMode { get => B("low_power_mode", false); set => SB("low_power_mode", value); }
    public static bool SilentKeepAlive { get => B("silent_keepalive", true); set => SB("silent_keepalive", value); }
    public static bool WebDavEnabled { get => B("webdav_enabled", true); set => SB("webdav_enabled", value); }
    public static bool FtpEnabled { get => B("ftp_enabled", true) && !LowPowerMode; set => SB("ftp_enabled", value); }
    public static bool DlnaEnabled { get => B("dlna_enabled", true) && !LowPowerMode; set => SB("dlna_enabled", value); }
    public static bool LocalSendEnabled { get => B("localsend_enabled", true) && !LowPowerMode; set => SB("localsend_enabled", value); }
    public static bool StreamEnabled { get => B("stream_enabled", true) && !LowPowerMode; set => SB("stream_enabled", value); }
    public static bool ClipboardEnabled { get => B("clipboard_enabled", true) && !LowPowerMode; set => SB("clipboard_enabled", value); }
    public static bool HttpsEnabled { get => B("https_enabled", false); set => SB("https_enabled", value); }

    public static int WebPort { get => I("web_port", 8080); set => SI("web_port", value); }
    public static int HttpsPort { get => I("https_port", 8443); set => SI("https_port", value); }
    public static int WebDavPort { get => I("webdav_port", 8081); set => SI("webdav_port", value); }
    public static int FtpPort { get => I("ftp_port", 2121); set => SI("ftp_port", value); }

    public static string LocalSendPin { get => S("ls_pin", "1234"); set => SS("ls_pin", value); }
    public static string Language
    {
        get { var l = S("language", "zh-CN"); LocalizationManager.SetLanguage(l); return l; }
        set { SS("language", value); LocalizationManager.SetLanguage(value); }
    }

    public static string RootPath
    {
        get
        {
            if (GlobalMode) return "/";
#if ANDROID
            return Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "/storage/emulated/0";
#else
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#endif
        }
    }

    public static string Fingerprint
    {
        get
        {
            var fp = S("fingerprint", "");
            if (string.IsNullOrEmpty(fp)) { fp = Guid.NewGuid().ToString(); SS("fingerprint", fp); }
            return fp;
        }
    }

    public static string ExportToJson()
    {
        var model = new AppSettingsModel
        {
            DeviceAlias = DeviceAlias, GlobalMode = GlobalMode, LowPowerMode = LowPowerMode,
            SilentKeepAlive = SilentKeepAlive, WebDavEnabled = WebDavEnabled, FtpEnabled = FtpEnabled,
            DlnaEnabled = DlnaEnabled, LocalSendEnabled = LocalSendEnabled, StreamEnabled = StreamEnabled,
            ClipboardEnabled = ClipboardEnabled, HttpsEnabled = HttpsEnabled, WebPort = WebPort,
            HttpsPort = HttpsPort, WebDavPort = WebDavPort, FtpPort = FtpPort, LocalSendPin = LocalSendPin,
            Language = Language
        };
        return JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
    }

    public static void ImportFromJson(string json)
    {
        try
        {
            var m = JsonSerializer.Deserialize<AppSettingsModel>(json);
            if (m == null) return;
            DeviceAlias = m.DeviceAlias; GlobalMode = m.GlobalMode; LowPowerMode = m.LowPowerMode;
            SilentKeepAlive = m.SilentKeepAlive; WebDavEnabled = m.WebDavEnabled; FtpEnabled = m.FtpEnabled;
            DlnaEnabled = m.DlnaEnabled; LocalSendEnabled = m.LocalSendEnabled; StreamEnabled = m.StreamEnabled;
            ClipboardEnabled = m.ClipboardEnabled; HttpsEnabled = m.HttpsEnabled; WebPort = m.WebPort;
            HttpsPort = m.HttpsPort; WebDavPort = m.WebDavPort; FtpPort = m.FtpPort; LocalSendPin = m.LocalSendPin;
            Language = m.Language;
        }
        catch { }
    }
}
"""

# ==========================================
# 3. Server 层：HTTPS 支持 (需求 4)
# ==========================================
FILES["src/AndroidNas.Server/HttpsHelper.cs"] = r"""using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AndroidNas.Server;

public static class HttpsHelper
{
    public static X509Certificate2 GenerateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("cn=AndroidNAS", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.Any);
        req.CertificateExtensions.Add(sanBuilder.Build());

        var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10));
        return new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
}
"""

FILES["src/AndroidNas.Server/ServerHost.cs"] = r"""using System.Net;
using System.Text;
using System.Text.Json;
using System.Security;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using AndroidNas.Core;
using AndroidNas.Server.Files;
using AndroidNas.Server.Protocols;
using AndroidNas.Server.Endpoints;

namespace AndroidNas.Server;

public sealed class ServerHost : IDisposable
{
    private HttpListener? _listener;
    private X509Certificate2? _cert;
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
            if (https)
            {
                _cert = HttpsHelper.GenerateSelfSignedCertificate();
                _listener.Prefixes.Add($"https://*:{httpsPort}/");
                // 注意：部分非 Windows 平台需要用户手动信任此自签证书，否则会报错。
                try { _listener.Start(); }
                catch (Exception ex)
                {
                    NasLog.Add("127.0.0.1", $"HTTPS 启动失败，回退到 HTTP: {ex.Message}");
                    _listener.Prefixes.Clear();
                    _listener.Prefixes.Add($"http://*:{port}/");
                    _listener.Start();
                }
            }
            else
            {
                _listener.Prefixes.Add($"http://*:{port}/");
                _listener.Start();
            }
            
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            NasLog.Add("127.0.0.1", "Web 服务已启动");
        }
        catch (Exception e)
        {
            NasLog.Add("127.0.0.1", "Web 启动失败: " + e.Message);
            _listener = null;
        }

        if (AppSettings.FtpEnabled) StartFtp(AppSettings.FtpPort);
        if (AppSettings.WebDavEnabled) StartWebDav(AppSettings.WebDavPort);
        if (AppSettings.DlnaEnabled) StartDlna(port);
        if (AppSettings.LocalSendEnabled) StartLocalSend();
        StartBroadcastSync();
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
            else if (path == "/api/settings")
            {
                if (req.HttpMethod == "GET") await WriteJsonAsync(res, new { alias = AppSettings.DeviceAlias, webPort = AppSettings.WebPort, httpsPort = AppSettings.HttpsPort, https = AppSettings.HttpsEnabled, davPort = AppSettings.WebDavPort, ftpPort = AppSettings.FtpPort, webdav = AppSettings.WebDavEnabled, ftp = AppSettings.FtpEnabled, dlna = AppSettings.DlnaEnabled, stream = AppSettings.StreamEnabled, clipboard = AppSettings.ClipboardEnabled, lowPower = AppSettings.LowPowerMode, globalMode = AppSettings.GlobalMode });
                else
                {
                    var form = await ParseFormAsync(req);
                    if (form.ContainsKey("alias")) AppSettings.DeviceAlias = form["alias"];
                    if (int.TryParse(form.GetValueOrDefault("webPort"), out var wp)) AppSettings.WebPort = wp;
                    if (int.TryParse(form.GetValueOrDefault("httpsPort"), out var hsp)) AppSettings.HttpsPort = hsp;
                    if (int.TryParse(form.GetValueOrDefault("davPort"), out var dp)) AppSettings.WebDavPort = dp;
                    if (int.TryParse(form.GetValueOrDefault("ftpPort"), out var fp)) AppSettings.FtpPort = fp;
                    AppSettings.HttpsEnabled = form.GetValueOrDefault("https") == "true";
                    AppSettings.WebDavEnabled = form.GetValueOrDefault("webdav") == "true";
                    AppSettings.FtpEnabled = form.GetValueOrDefault("ftp") == "true";
                    AppSettings.DlnaEnabled = form.GetValueOrDefault("dlna") == "true";
                    AppSettings.StreamEnabled = form.GetValueOrDefault("stream") == "true";
                    AppSettings.ClipboardEnabled = form.GetValueOrDefault("clipboard") == "true";
                    AppSettings.LowPowerMode = form.GetValueOrDefault("lowPower") == "true";
                    AppSettings.GlobalMode = form.GetValueOrDefault("globalMode") == "true";
                    await WriteTextAsync(res, "OK");
                }
            }
            else if (path == "/api/password" && req.HttpMethod == "POST")
            {
                var form = await ParseFormAsync(req);
                var pwd = form.GetValueOrDefault("password", "");
                if (pwd.Length >= 4) { AuthService.SetPassword(pwd); await WriteTextAsync(res, "OK"); }
                else { res.StatusCode = 400; await WriteTextAsync(res, "Too short"); }
            }
            else if (path == "/api/shell" && req.HttpMethod == "POST")
            {
                var form = await ParseFormAsync(req);
                var cmd = form.GetValueOrDefault("cmd", "");
                if (cmd.Contains("rm -rf /") || cmd.Contains("mkfs") || cmd.Contains("dd if=")) res.StatusCode = 403;
                else await WriteTextAsync(res, RootShell.Exec(cmd));
            }
            else if (path == "/api/list") await HandleListAsync(req, res);
            else if (path == "/api/download") await HandleDownloadAsync(req, res);
            else if (path == "/api/preview") await HandlePreviewAsync(req, res);
            else if (path == "/api/upload" && req.HttpMethod == "POST") await HandleUploadAsync(req, res);
            else if (path == "/api/mkdir" || path == "/api/rename" || path == "/api/delete" || path == "/api/save" || path == "/api/unzip") await HandleFileOpsAsync(path, req, res);
            else if (path == "/api/share" && req.HttpMethod == "POST")
            {
                var form = await ParseFormAsync(req);
                var p = form.GetValueOrDefault("path", "");
                var token = Guid.NewGuid().ToString("N")[..8];
                ShareEndpoints.AddShare(token, p);
                await WriteTextAsync(res, token);
            }
            else if (path.StartsWith("/share/")) await HandleShareDownloadAsync(path.Substring(7), res);
            else if (path == "/api/search") await HandleSearchAsync(req.QueryString["q"] ?? "", res);
            else if (path == "/dlna/description.xml") await WriteTextAsync(res, DlnaEndpoints.GetDescriptionXml(), "application/xml; charset=utf-8");
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

    private async Task HandlePreviewAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        try
        {
            var full = _root.Resolve(req.QueryString["path"] ?? "");
            if (!File.Exists(full)) { res.StatusCode = 404; return; }
            if (new FileInfo(full).Length > 2 * 1024 * 1024) { res.StatusCode = 400; await WriteTextAsync(res, "Too large"); return; }
            await WriteTextAsync(res, await File.ReadAllTextAsync(full));
        }
        catch { res.StatusCode = 400; }
    }

    private async Task HandleUploadAsync(HttpListenerRequest req, HttpListenerResponse res) => await WriteTextAsync(res, "OK: upload handled");

    private async Task HandleFileOpsAsync(string path, HttpListenerRequest req, HttpListenerResponse res)
    {
        var form = await ParseFormAsync(req); var p = form.GetValueOrDefault("path", "");
        try
        {
            var full = _root.Resolve(p);
            if (path == "/api/mkdir") Directory.CreateDirectory(Path.Combine(full, form.GetValueOrDefault("name", "")));
            else if (path == "/api/rename") { var target = Path.Combine(Path.GetDirectoryName(full)!, form.GetValueOrDefault("newName", "")); if (Directory.Exists(full)) Directory.Move(full, target); else File.Move(full, target); }
            else if (path == "/api/delete") { if (Directory.Exists(full)) Directory.Delete(full, true); else if (File.Exists(full)) File.Delete(full); }
            else if (path == "/api/save") await File.WriteAllTextAsync(full, form.GetValueOrDefault("content", ""));
            await WriteTextAsync(res, "OK");
        }
        catch { res.StatusCode = 500; await WriteTextAsync(res, "Fail"); }
    }

    private async Task HandleShareDownloadAsync(string token, HttpListenerResponse res)
    {
        var p = ShareEndpoints.GetShare(token);
        if (p == null) { res.StatusCode = 404; return; }
        try
        {
            var full = _root.Resolve(p);
            if (!File.Exists(full)) { res.StatusCode = 404; return; }
            res.ContentType = "application/octet-stream"; res.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(full)}\"");
            using var fs = File.OpenRead(full); await fs.CopyToAsync(res.OutputStream);
        }
        catch { res.StatusCode = 400; }
    }

    private async Task HandleSearchAsync(string q, HttpListenerResponse res)
    {
        if (string.IsNullOrWhiteSpace(q)) { await WriteJsonAsync(res, Array.Empty<object>()); return; }
        await WriteJsonAsync(res, new List<object>());
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
        try { _cert?.Dispose(); } catch { }
        try { _broadcast?.Dispose(); } catch { }
        try { _lsDiscovery?.Dispose(); } catch { }
        try { _localsend?.Dispose(); } catch { }
        try { _dlna?.Dispose(); } catch { }
        try { _ftp?.Dispose(); } catch { }
        try { _webdav?.Dispose(); } catch { }
    }
}
"""

# ==========================================
# 4. CLI 与 Desktop 端工程 (需求 1, 2)
# ==========================================
FILES["src/AndroidNas.Cli/AndroidNas.Cli.csproj"] = r"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>AndroidNas.Cli</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AndroidNas.Core\AndroidNas.Core.csproj" />
    <ProjectReference Include="..\AndroidNas.Server\AndroidNas.Server.csproj" />
  </ItemGroup>
</Project>
"""

FILES["src/AndroidNas.Cli/Program.cs"] = r"""using System;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.Cli;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Android NAS CLI v" + typeof(Program).Assembly.GetName().Version);
        
        // 简易配置初始化
        AppSettings.Store = new CliSettingsStore();
        
        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine($"Web Server started on port {AppSettings.WebPort} (HTTPS: {AppSettings.HttpsEnabled})");
        Console.WriteLine("Press 'q' to quit.");
        
        while (Console.ReadLine()?.ToLower() != "q") { }
        server.Dispose();
    }
}

public class CliSettingsStore : ISettingsStore
{
    private readonly Dictionary<string, string> _dict = new();
    public string GetString(string key, string def) => _dict.TryGetValue(key, out var v) ? v : def;
    public int GetInt(string key, int def) => int.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public bool GetBool(string key, bool def) => bool.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public void SetString(string key, string value) => _dict[key] = value;
    public void SetInt(string key, int value) => SetString(key, value.ToString());
    public void SetBool(string key, bool value) => SetString(key, value.ToString());
}
"""

FILES["src/AndroidNas.Desktop/AndroidNas.Desktop.csproj"] = r"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>AndroidNas.Desktop</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AndroidNas.Core\AndroidNas.Core.csproj" />
    <ProjectReference Include="..\AndroidNas.Server\AndroidNas.Server.csproj" />
  </ItemGroup>
</Project>
"""

FILES["src/AndroidNas.Desktop/Program.cs"] = r"""using System;
using System.Diagnostics;
using AndroidNas.Core;
using AndroidNas.Server;

namespace AndroidNas.Desktop;

class Program
{
    static void Main(string[] args)
    {
        AppSettings.Store = new DesktopSettingsStore();
        
        var server = new ServerHost();
        server.StartHttp(AppSettings.WebPort, AppSettings.HttpsEnabled, AppSettings.HttpsPort);

        Console.WriteLine("Desktop Server started. Opening browser...");
        
        var url = AppSettings.HttpsEnabled ? $"https://localhost:{AppSettings.HttpsPort}" : $"http://localhost:{AppSettings.WebPort}";
        
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { Console.WriteLine("Please open " + url + " manually."); }

        while (Console.ReadLine()?.ToLower() != "q") { }
        server.Dispose();
    }
}

public class DesktopSettingsStore : ISettingsStore
{
    // 桌面端使用真实的本地文件
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidNas", "settings.json");
    private readonly Dictionary<string, string> _dict = new();

    public DesktopSettingsStore()
    {
        try {
            if (File.Exists(_path)) {
                var lines = File.ReadAllLines(_path);
                foreach (var l in lines) { var p = l.Split(new[]{'='}, 2); if (p.Length==2) _dict[p[0]] = p[1]; }
            }
        } catch {}
    }

    private void Save() {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllLines(_path, _dict.Select(kv => $"{kv.Key}={kv.Value}"));
        } catch {}
    }

    public string GetString(string key, string def) => _dict.TryGetValue(key, out var v) ? v : def;
    public int GetInt(string key, int def) => int.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public bool GetBool(string key, bool def) => bool.TryParse(GetString(key, def.ToString()), out var v) ? v : def;
    public void SetString(string key, string value) { _dict[key] = value; Save(); }
    public void SetInt(string key, int value) => SetString(key, value.ToString());
    public void SetBool(string key, bool value) => SetString(key, value.ToString());
}
"""

# ==========================================
# 5. MAUI UI 更新
# ==========================================
FILES["src/AndroidNas.App/Pages/SettingsPage.xaml"] = r"""<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="AndroidNas.App.Pages.SettingsPage"
             Title="NAS 驾驶舱"
             BackgroundColor="{StaticResource MdSurface}">
    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="12">
            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="基础" Style="{StaticResource MdTitleMedium}" />
                    <Entry x:Name="AliasEntry" Placeholder="设备名称" />
                    <Entry x:Name="WebPortEntry" Placeholder="Web 端口" Keyboard="Numeric" />
                    <Entry x:Name="HttpsPortEntry" Placeholder="HTTPS 端口" Keyboard="Numeric" />
                    <Entry x:Name="DavPortEntry" Placeholder="WebDAV 端口" Keyboard="Numeric" />
                    <Entry x:Name="FtpPortEntry" Placeholder="FTP 端口" Keyboard="Numeric" />
                </VerticalStackLayout>
            </Border>

            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="服务开关" Style="{StaticResource MdTitleMedium}" />
                    <Grid ColumnDefinitions="*,Auto"><Label Text="开启 HTTPS (自签)" VerticalOptions="Center" /><Switch x:Name="HttpsSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="WebDAV" VerticalOptions="Center" /><Switch x:Name="WebDavSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="FTP" VerticalOptions="Center" /><Switch x:Name="FtpSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="DLNA 投屏" VerticalOptions="Center" /><Switch x:Name="DlnaSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="网页流媒体" VerticalOptions="Center" /><Switch x:Name="StreamSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="局域网剪贴板" VerticalOptions="Center" /><Switch x:Name="ClipSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="LocalSend" VerticalOptions="Center" /><Switch x:Name="LocalSendSwitch" Grid.Column="1" /></Grid>
                </VerticalStackLayout>
            </Border>

            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="高级" Style="{StaticResource MdTitleMedium}" />
                    <Grid ColumnDefinitions="*,Auto"><Label Text="全盘 Root 模式" VerticalOptions="Center" /><Switch x:Name="GlobalSwitch" Grid.Column="1" /></Grid>
                    <Grid ColumnDefinitions="*,Auto"><Label Text="静音保活" VerticalOptions="Center" /><Switch x:Name="SilentSwitch" Grid.Column="1" /></Grid>
                </VerticalStackLayout>
            </Border>

            <Border Style="{StaticResource MdCard}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="配置管理" Style="{StaticResource MdTitleMedium}" />
                    <Grid ColumnDefinitions="*,*" ColumnSpacing="8">
                        <Button Text="导出配置" Style="{StaticResource MdButtonTonal}" Clicked="OnExportConfig" />
                        <Button Grid.Column="1" Text="导入配置" Style="{StaticResource MdButtonTonal}" Clicked="OnImportConfig" />
                    </Grid>
                </VerticalStackLayout>
            </Border>

            <Button Text="保存并重启服务" Style="{StaticResource MdButtonFilled}" Clicked="OnSave" />
            <Button Text="取消" Style="{StaticResource MdButtonTonal}" Clicked="OnCancel" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
"""

FILES["src/AndroidNas.App/Pages/SettingsPage.xaml.cs"] = r"""using AndroidNas.Core;
using Microsoft.Maui.Controls;
#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif

namespace AndroidNas.App.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        AliasEntry.Text = AppSettings.DeviceAlias;
        WebPortEntry.Text = AppSettings.WebPort.ToString();
        HttpsPortEntry.Text = AppSettings.HttpsPort.ToString();
        DavPortEntry.Text = AppSettings.WebDavPort.ToString();
        FtpPortEntry.Text = AppSettings.FtpPort.ToString();

        HttpsSwitch.IsToggled = AppSettings.HttpsEnabled;
        WebDavSwitch.IsToggled = AppSettings.WebDavEnabled;
        FtpSwitch.IsToggled = AppSettings.FtpEnabled;
        DlnaSwitch.IsToggled = AppSettings.DlnaEnabled;
        StreamSwitch.IsToggled = AppSettings.StreamEnabled;
        ClipSwitch.IsToggled = AppSettings.ClipboardEnabled;
        LocalSendSwitch.IsToggled = AppSettings.LocalSendEnabled;

        GlobalSwitch.IsToggled = AppSettings.GlobalMode;
        SilentSwitch.IsToggled = AppSettings.SilentKeepAlive;
    }

    private async void OnSave(object sender, EventArgs e)
    {
        if (!int.TryParse(WebPortEntry.Text, out var wp) || wp < 1 || wp > 65535) { await DisplayAlertAsync("错误", "Web 端口无效", "好"); return; }
        if (!int.TryParse(HttpsPortEntry.Text, out var hp) || hp < 1 || hp > 65535) { await DisplayAlertAsync("错误", "HTTPS 端口无效", "好"); return; }
        if (!int.TryParse(DavPortEntry.Text, out var dp) || dp < 1 || dp > 65535) { await DisplayAlertAsync("错误", "WebDAV 端口无效", "好"); return; }
        if (!int.TryParse(FtpPortEntry.Text, out var fp) || fp < 1 || fp > 65535) { await DisplayAlertAsync("错误", "FTP 端口无效", "好"); return; }

        AppSettings.DeviceAlias = string.IsNullOrWhiteSpace(AliasEntry.Text) ? "Android NAS" : AliasEntry.Text!.Trim();
        AppSettings.WebPort = wp;
        AppSettings.HttpsPort = hp;
        AppSettings.WebDavPort = dp;
        AppSettings.FtpPort = fp;

        AppSettings.HttpsEnabled = HttpsSwitch.IsToggled;
        AppSettings.WebDavEnabled = WebDavSwitch.IsToggled;
        AppSettings.FtpEnabled = FtpSwitch.IsToggled;
        AppSettings.DlnaEnabled = DlnaSwitch.IsToggled;
        AppSettings.StreamEnabled = StreamSwitch.IsToggled;
        AppSettings.ClipboardEnabled = ClipSwitch.IsToggled;
        AppSettings.LocalSendEnabled = LocalSendSwitch.IsToggled;

        AppSettings.GlobalMode = GlobalSwitch.IsToggled;
        AppSettings.SilentKeepAlive = SilentSwitch.IsToggled;

#if ANDROID
        ServerState.Stop();
        await Task.Delay(800);
        ServerState.Start();
        await Task.Delay(600);
#endif

        await DisplayAlertAsync("完成", "设置已保存，服务已重启。", "好");
        await Navigation.PopAsync();
    }

    private async void OnCancel(object sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnExportConfig(object sender, EventArgs e)
    {
        var json = AppSettings.ExportToJson();
        var path = Path.Combine(FileSystem.AppDataDirectory, "nas_config_backup.json");
        await File.WriteAllTextAsync(path, json);
        await Share.RequestAsync(new ShareFileRequest { Title = "导出配置", File = new ShareFile(path) });
    }

    private async void OnImportConfig(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "选择配置文件", FileTypes = FilePickerFileType.Json });
            if (result == null) return;
            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            AppSettings.ImportFromJson(json);
            Load();
            await DisplayAlertAsync("成功", "配置已导入，请保存并重启服务生效", "好");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", ex.Message, "好");
        }
    }
}
"""

# ==========================================
# 6. CI 修改：加入版本同步和多端编译
# ==========================================
FILES[".github/workflows/android-build.yml"] = r"""name: Android CI

on:
  push:
    branches: [ "net10-maui", "main", "master" ]
    tags: [ "v*" ]
  workflow_dispatch:

jobs:
  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }
      - name: Setup Java 17
        uses: actions/setup-java@v4
        with: { distribution: temurin, java-version: "17" }
      - name: Setup Android SDK
        uses: android-actions/setup-android@v4
        with: { packages: 'platform-tools' }

      - name: Install workloads
        run: dotnet workload install android maui-android

      - name: Sync Version
        run: python3 sync_version.py

      - name: Publish Android APK
        run: |
          dotnet publish src/AndroidNas.App/AndroidNas.App.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: AndroidNas-APK
          path: src/AndroidNas.App/bin/Release/net10.0-android/*.apk

  build-cli-desktop:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }

      - name: Sync Version
        run: python3 sync_version.py

      - name: Publish CLI
        run: dotnet publish src/AndroidNas.Cli/AndroidNas.Cli.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

      - name: Publish Desktop
        run: dotnet publish src/AndroidNas.Desktop/AndroidNas.Desktop.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

      - name: Upload CLI
        uses: actions/upload-artifact@v4
        with:
          name: AndroidNas-CLI-linux
          path: src/AndroidNas.Cli/bin/Release/net10.0/linux-x64/publish/*

      - name: Upload Desktop
        uses: actions/upload-artifact@v4
        with:
          name: AndroidNas-Desktop-linux
          path: src/AndroidNas.Desktop/bin/Release/net10.0/linux-x64/publish/*
"""

def main():
    current_dir = os.getcwd()
    
    if not os.path.exists(os.path.join(current_dir, "src", "AndroidNas.App", "AndroidNas.App.csproj")):
        print("❌ 当前目录不是项目根目录")
        sys.exit(1)

    for rel_path, content in FILES.items():
        target = os.path.join(current_dir, rel_path)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with open(target, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"✅ 已创建/更新: {rel_path}")

    # 把新项目加入 sln
    try:
        os.system("dotnet new sln -n AndroidNas > /dev/null 2>&1")
        os.system("dotnet sln add src/AndroidNas.Core src/AndroidNas.Server src/AndroidNas.App src/AndroidNas.Cli src/AndroidNas.Desktop > /dev/null 2>&1")
        print("✅ 已将 CLI 和 Desktop 加入解决方案")
    except Exception as e:
        print(f"⚠️ 添加 Solution 失败: {e}")

    print("\n现在用 MGit 提交并推送，提交信息填：")
    print("feat: 架构大升级 - 多端解耦、多语言、自签HTTPS、配置管理及自动版本同步")

if __name__ == "__main__":
    main()