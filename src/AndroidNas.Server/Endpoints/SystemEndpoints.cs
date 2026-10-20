using AndroidNas.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AndroidNas.Server.Endpoints;

public static class SystemEndpoints
{
    private static string _clipboard = "";

    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/status", () => Results.Ok(new
        {
            lowPower = AppSettings.LowPowerMode,
            clipboardEnabled = AppSettings.ClipboardEnabled,
            globalMode = AppSettings.GlobalMode
        }));

        app.MapGet("/api/clipboard", () =>
            !AppSettings.ClipboardEnabled ? Results.StatusCode(403) : Results.Text(_clipboard));

        app.MapPost("/api/clipboard", async (HttpRequest req) =>
        {
            if (!AppSettings.ClipboardEnabled) return Results.StatusCode(403);
            var form = await req.ReadFormAsync();
            _clipboard = form["content"].ToString();
            return Results.Text("OK");
        });

        app.MapGet("/api/settings", () => Results.Ok(new
        {
            alias = AppSettings.DeviceAlias,
            webPort = AppSettings.WebPort,
            davPort = AppSettings.WebDavPort,
            ftpPort = AppSettings.FtpPort,
            webdav = AppSettings.WebDavEnabled,
            ftp = AppSettings.FtpEnabled,
            dlna = AppSettings.DlnaEnabled,
            stream = AppSettings.StreamEnabled,
            clipboard = AppSettings.ClipboardEnabled,
            lowPower = AppSettings.LowPowerMode,
            globalMode = AppSettings.GlobalMode
        }));

        app.MapPost("/api/settings", async (HttpRequest req) =>
        {
            var form = await req.ReadFormAsync();
            if (form.ContainsKey("alias")) AppSettings.DeviceAlias = form["alias"].ToString();
            if (form.ContainsKey("webPort") && int.TryParse(form["webPort"], out var wp)) AppSettings.WebPort = wp;
            if (form.ContainsKey("davPort") && int.TryParse(form["davPort"], out var dp)) AppSettings.WebDavPort = dp;
            if (form.ContainsKey("ftpPort") && int.TryParse(form["ftpPort"], out var fp)) AppSettings.FtpPort = fp;
            if (form.ContainsKey("webdav"))    AppSettings.WebDavEnabled    = form["webdav"].ToString() == "true";
            if (form.ContainsKey("ftp"))       AppSettings.FtpEnabled       = form["ftp"].ToString() == "true";
            if (form.ContainsKey("dlna"))      AppSettings.DlnaEnabled      = form["dlna"].ToString() == "true";
            if (form.ContainsKey("stream"))    AppSettings.StreamEnabled    = form["stream"].ToString() == "true";
            if (form.ContainsKey("clipboard")) AppSettings.ClipboardEnabled = form["clipboard"].ToString() == "true";
            if (form.ContainsKey("lowPower"))  AppSettings.LowPowerMode     = form["lowPower"].ToString() == "true";
            if (form.ContainsKey("globalMode"))AppSettings.GlobalMode       = form["globalMode"].ToString() == "true";
            return Results.Text("OK");
        });

        app.MapPost("/api/password", async (HttpRequest req) =>
        {
            var form = await req.ReadFormAsync();
            var newPass = form["password"].ToString();
            if (string.IsNullOrEmpty(newPass) || newPass.Length < 4) return Results.BadRequest("Too short");
            AuthService.SetPassword(newPass);
            return Results.Text("OK");
        });

        app.MapPost("/api/shell", async (HttpRequest req) =>
        {
            var form = await req.ReadFormAsync();
            var cmd = form["cmd"].ToString();
            if (string.IsNullOrEmpty(cmd)) return Results.BadRequest("No command");
            if (cmd.Contains("rm -rf /") || cmd.Contains("mkfs") || cmd.Contains("dd if="))
                return Results.StatusCode(403);
            return Results.Text(RootShell.Exec(cmd));
        });

        return app;
    }
}
