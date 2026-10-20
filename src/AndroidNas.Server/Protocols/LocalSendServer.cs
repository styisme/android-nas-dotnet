using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using AndroidNas.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AndroidNas.Server.Protocols;

public sealed class LocalSendServer : IDisposable
{
    private const int Port = 53317;
    private WebApplication? _app;
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private Timer? _cleanupTimer;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://0.0.0.0:" + Port);

            var app = builder.Build();

            app.MapPost("/api/localsend/v2/register", () =>
                Results.Json(BuildDeviceInfo()));

            app.MapPost("/api/localsend/v2/prepare-upload", async (HttpContext ctx) =>
            {
                using var reader = new StreamReader(ctx.Request.Body);
                var body = await reader.ReadToEndAsync();
                try
                {
                    var json = JsonNode.Parse(body);
                    var pin = json?["pin"]?.GetValue<string>() ?? "";
                    if (pin != AppSettings.LocalSendPin)
                    {
                        ctx.Response.Headers["X-LocalSend-Pin"] = "true";
                        return Results.Json(new { error = "PIN required" }, statusCode: 401);
                    }

                    var files = json?["files"]?.AsObject();
                    if (files == null) return Results.BadRequest("No files");

                    var sessionId = Guid.NewGuid().ToString();
                    var session = new Session(sessionId);
                    var resFiles = new JsonObject();

                    foreach (var kv in files)
                    {
                        var fileId = kv.Key;
                        var fileName = kv.Value?["fileName"]?.GetValue<string>() ?? ("unknown_" + fileId);
                        var token = Guid.NewGuid().ToString();
                        session.FileTokens[fileId] = token;
                        session.FileNames[fileId] = fileName;
                        resFiles[fileId] = token;
                    }

                    session.LastActive = DateTime.UtcNow;
                    _sessions[sessionId] = session;

                    return Results.Json(new { sessionId, files = resFiles });
                }
                catch (Exception e)
                {
                    return Results.Json(new { error = e.Message }, statusCode: 500);
                }
            });

            app.MapPost("/api/localsend/v2/upload", async (HttpContext ctx) =>
            {
                var sessionId = ctx.Request.Query["sessionId"].ToString();
                var fileId = ctx.Request.Query["fileId"].ToString();
                var token = ctx.Request.Query["token"].ToString();

                if (!_sessions.TryGetValue(sessionId, out var session))
                    return Results.Json(new { error = "Invalid session" }, statusCode: 401);

                if (!session.FileTokens.TryGetValue(fileId, out var expected) || token != expected)
                    return Results.Json(new { error = "Invalid token" }, statusCode: 401);

                session.LastActive = DateTime.UtcNow;
                var fileName = session.FileNames.TryGetValue(fileId, out var fn)
                    ? fn
                    : ("ls_" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

                var saveDir = Path.Combine(AppSettings.RootPath, "Download", "LocalSend");
                Directory.CreateDirectory(saveDir);

                var destFile = Path.Combine(saveDir, SanitizeFileName(fileName));
                if (File.Exists(destFile))
                {
                    var baseName = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    var n = 1;
                    while (File.Exists(destFile))
                    {
                        destFile = Path.Combine(saveDir, baseName + "(" + n + ")" + ext);
                        n++;
                    }
                }

                await using (var fs = File.Create(destFile))
                    await ctx.Request.Body.CopyToAsync(fs);

                session.FileTokens.TryRemove(fileId, out _);
                session.FileNames.TryRemove(fileId, out _);
                if (session.FileTokens.IsEmpty)
                    _sessions.TryRemove(sessionId, out _);

                NasLog.Add("LocalSend", "收到文件: " + Path.GetFileName(destFile));
                return Results.Text("OK");
            });

            app.MapPost("/api/localsend/v2/cancel", (HttpContext ctx) =>
            {
                var sessionId = ctx.Request.Query["sessionId"].ToString();
                if (!string.IsNullOrEmpty(sessionId))
                    _sessions.TryRemove(sessionId, out _);
                return Results.Text("Cancelled");
            });

            _app = app;
            _ = app.StartAsync();
            IsRunning = true;

            _cleanupTimer = new Timer(CleanupSessions, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            NasLog.Add("127.0.0.1", "LocalSend 已启动: " + Port);
        }
        catch (Exception e)
        {
            NasLog.Add("127.0.0.1", "LocalSend 启动失败: " + e.Message);
            Dispose();
        }
    }

    private void CleanupSessions(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _sessions)
            if ((now - kv.Value.LastActive).TotalMinutes > 5)
                _sessions.TryRemove(kv.Key, out _);
    }

    private object BuildDeviceInfo() => new
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

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "ls_file" : name;
    }

    public void Dispose()
    {
        if (!IsRunning) return;
        IsRunning = false;
        try { _cleanupTimer?.Dispose(); } catch { }
        _cleanupTimer = null;
        try { _app?.StopAsync().GetAwaiter().GetResult(); } catch { }
        _app = null;
        _sessions.Clear();
        NasLog.Add("127.0.0.1", "LocalSend 已停止");
    }

    private sealed class Session
    {
        public string Id { get; }
        public ConcurrentDictionary<string, string> FileTokens { get; } = new();
        public ConcurrentDictionary<string, string> FileNames { get; } = new();
        public DateTime LastActive { get; set; }
        public Session(string id) { Id = id; LastActive = DateTime.UtcNow; }
    }
}
