using System.Text;
using AndroidNas.Core;
using AndroidNas.Server.Auth;
using AndroidNas.Server.Endpoints;
using AndroidNas.Server.Files;
using AndroidNas.Server.Protocols;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace AndroidNas.Server;

public sealed class ServerHost : IDisposable
{
    private WebApplication? _app;
    private FtpServer? _ftp;
    private WebDavServer? _webdav;
    private DlnaServer? _dlna;
    private LocalSendServer? _localsend;
    private LocalSendDiscovery? _lsDiscovery;
    private BroadcastSyncer? _broadcast;

    public void StartHttp(int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://0.0.0.0:" + port);

        builder.Services.AddSingleton<IAuthConfig, DefaultAuthConfig>();
        builder.Services.AddSingleton<IFileRootProvider>(_ =>
            new PhysicalRootProvider(AppSettings.RootPath, AppSettings.GlobalMode));

        var app = builder.Build();
        app.UseMiddleware<BasicAuthMiddleware>();

        // 物理 wwwroot（若存在）
        var wwwRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (Directory.Exists(wwwRoot))
        {
            var fp = new PhysicalFileProvider(wwwRoot);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fp });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = fp });
        }

        // 内嵌资源路由（兜底）
        app.MapGet("/", (HttpContext ctx) => ServeEmbeddedIndex());
        app.MapGet("/index.html", (HttpContext ctx) => ServeEmbeddedIndex());

        app.MapSystemEndpoints();
        app.MapFileEndpoints();
        app.MapShareEndpoints();
        app.MapSearchEndpoints();
        app.MapDlnaEndpoints();

        _app = app;
        _ = app.StartAsync();
        NasLog.Add("127.0.0.1", "Web 服务已启动: " + port);

        if (AppSettings.FtpEnabled) StartFtp(AppSettings.FtpPort);
        if (AppSettings.WebDavEnabled) StartWebDav(AppSettings.WebDavPort);
        if (AppSettings.DlnaEnabled) StartDlna(port);
        if (AppSettings.LocalSendEnabled) StartLocalSend();
        StartBroadcastSync();
    }

    private static IResult ServeEmbeddedIndex()
    {
        try
        {
            var asm = typeof(ServerHost).Assembly;
            var name = "AndroidNas.Server.Resources.wwwroot.index.html";
            var stream = asm.GetManifestResourceStream(name);
            if (stream == null)
            {
                return Results.Content(
                    "<html><body><h1>Android NAS Next</h1><p>Web UI 未打包</p></body></html>",
                    "text/html", Encoding.UTF8);
            }
            return Results.Stream(stream, "text/html", encoding: Encoding.UTF8);
        }
        catch (Exception e)
        {
            return Results.Content("错误: " + e.Message, "text/plain");
        }
    }

    public void StartFtp(int port)
    {
        if (_ftp != null) return;
        try { _ftp = new FtpServer(port, AppSettings.RootPath); _ftp.Start(); }
        catch (Exception e) { NasLog.Add("127.0.0.1", "FTP 启动失败: " + e.Message); _ftp = null; }
    }

    public void StartWebDav(int port)
    {
        if (_webdav != null) return;
        try { _webdav = new WebDavServer(port, AppSettings.RootPath); _webdav.Start(); }
        catch (Exception e) { NasLog.Add("127.0.0.1", "WebDAV 启动失败: " + e.Message); _webdav = null; }
    }

    public void StartDlna(int webPort)
    {
        if (_dlna != null) return;
        try { _dlna = new DlnaServer(webPort); _dlna.Start(); }
        catch (Exception e) { NasLog.Add("127.0.0.1", "DLNA 启动失败: " + e.Message); _dlna = null; }
    }

    public void StartLocalSend()
    {
        if (_localsend != null) return;
        try
        {
            _localsend = new LocalSendServer();
            _localsend.Start();
            _lsDiscovery = new LocalSendDiscovery();
            _lsDiscovery.Start();
        }
        catch (Exception e) { NasLog.Add("127.0.0.1", "LocalSend 启动失败: " + e.Message); }
    }

    public void StartBroadcastSync()
    {
        if (_broadcast != null) return;
        try { _broadcast = new BroadcastSyncer(); _broadcast.Start(); }
        catch (Exception e) { NasLog.Add("127.0.0.1", "UDP 8090 启动失败: " + e.Message); }
    }

    public void Dispose()
    {
        try { _broadcast?.Dispose(); } catch { }
        try { _lsDiscovery?.Dispose(); } catch { }
        try { _localsend?.Dispose(); } catch { }
        try { _dlna?.Dispose(); } catch { }
        try { _ftp?.Dispose(); } catch { }
        try { _webdav?.Dispose(); } catch { }
        try { _app?.StopAsync().GetAwaiter().GetResult(); } catch { }
    }
}
