#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import sys

FILES = {}

# 用原生 HttpListener 重写 LocalSendServer
FILES["src/AndroidNas.Server/Protocols/LocalSendServer.cs"] = r"""using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AndroidNas.Core;

namespace AndroidNas.Server.Protocols;

public sealed class LocalSendServer : IDisposable
{
    private const int Port = 53317;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private Timer? _cleanupTimer;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        try
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{Port}/");
            _listener.Start();
            IsRunning = true;
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            
            _cleanupTimer = new Timer(CleanupSessions, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            NasLog.Add("127.0.0.1", "LocalSend 已启动: " + Port);
        }
        catch (Exception e)
        {
            NasLog.Add("127.0.0.1", "LocalSend 启动失败: " + e.Message);
            Dispose();
        }
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
            res.AddHeader("Access-Control-Allow-Origin", "*");

            if (path == "/api/localsend/v2/register" && req.HttpMethod == "POST")
            {
                await WriteJsonAsync(res, BuildDeviceInfo());
            }
            else if (path == "/api/localsend/v2/prepare-upload" && req.HttpMethod == "POST")
            {
                await HandlePrepareUploadAsync(req, res);
            }
            else if (path == "/api/localsend/v2/upload" && req.HttpMethod == "POST")
            {
                await HandleUploadAsync(req, res);
            }
            else if (path == "/api/localsend/v2/cancel")
            {
                var sessionId = req.QueryString["sessionId"] ?? "";
                if (!string.IsNullOrEmpty(sessionId))
                    _sessions.TryRemove(sessionId, out _);
                await WriteTextAsync(res, "Cancelled");
            }
            else
            {
                res.StatusCode = 404;
                res.Close();
            }
        }
        catch
        {
            try { res.StatusCode = 500; res.Close(); } catch { }
        }
    }

    private async Task HandlePrepareUploadAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        try
        {
            var json = JsonNode.Parse(body);
            var pin = json?["pin"]?.GetValue<string>() ?? "";
            if (pin != AppSettings.LocalSendPin)
            {
                res.AddHeader("X-LocalSend-Pin", "true");
                res.StatusCode = 401;
                await WriteJsonAsync(res, new { error = "PIN required" });
                return;
            }

            var files = json?["files"]?.AsObject();
            if (files == null)
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new { error = "No files" });
                return;
            }

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

            await WriteJsonAsync(res, new { sessionId, files = resFiles });
        }
        catch (Exception e)
        {
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = e.Message });
        }
    }

    private async Task HandleUploadAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        var sessionId = req.QueryString["sessionId"] ?? "";
        var fileId = req.QueryString["fileId"] ?? "";
        var token = req.QueryString["token"] ?? "";

        if (!_sessions.TryGetValue(sessionId, out var session) || 
            !session.FileTokens.TryGetValue(fileId, out var expected) || token != expected)
        {
            res.StatusCode = 401;
            await WriteJsonAsync(res, new { error = "Invalid token" });
            return;
        }

        session.LastActive = DateTime.UtcNow;
        var fileName = session.FileNames.TryGetValue(fileId, out var fn) ? fn : ("ls_" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

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

        using (var fs = File.Create(destFile))
        {
            await req.InputStream.CopyToAsync(fs);
        }

        session.FileTokens.TryRemove(fileId, out _);
        session.FileNames.TryRemove(fileId, out _);
        if (session.FileTokens.IsEmpty)
            _sessions.TryRemove(sessionId, out _);

        NasLog.Add("LocalSend", "收到文件: " + Path.GetFileName(destFile));
        await WriteTextAsync(res, "OK");
    }

    private async Task WriteTextAsync(HttpListenerResponse res, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        res.ContentType = "text/plain; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private async Task WriteJsonAsync(HttpListenerResponse res, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        if (res.StatusCode != 401) res.Close(); // 401 留给外层处理
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
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "ls_file" : name;
    }

    public void Dispose()
    {
        if (!IsRunning) return;
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _listener = null;
        try { _cleanupTimer?.Dispose(); } catch { }
        _cleanupTimer = null;
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
        print(f"✅ 已重写: {rel_path}")

    print("\n现在用 MGit 提交并推送，提交信息填：")
    print("fix: 使用原生 HttpListener 重写 LocalSend 彻底移除 AspNetCore 依赖")

if __name__ == "__main__":
    main()