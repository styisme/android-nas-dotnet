using AndroidNas.Core;

namespace AndroidNas.Server.Auth;

public interface IAuthConfig
{
    bool Enabled { get; }
    bool Validate(string user, string pass);
}

public sealed class DefaultAuthConfig : IAuthConfig
{
    public bool Enabled => AuthService.Enabled;
    public bool Validate(string user, string pass) =>
        string.Equals(user, AuthService.Username, StringComparison.Ordinal) && pass == AuthService.Password;
}

public sealed class BasicAuthMiddleware(RequestDelegate next, IAuthConfig cfg)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!cfg.Enabled || ctx.Request.Path.StartsWithSegments("/share"))
        {
            await next(ctx);
            return;
        }

        var header = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            await Challenge(ctx);
            return;
        }

        try
        {
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(header[6..].Trim()));
            var idx = raw.IndexOf(':');
            if (idx <= 0) { await Challenge(ctx); return; }
            if (!cfg.Validate(raw[..idx], raw[(idx + 1)..])) { await Challenge(ctx); return; }
        }
        catch (FormatException) { await Challenge(ctx); return; }

        await next(ctx);
    }

    private static Task Challenge(HttpContext ctx)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.Headers.WWWAuthenticate = "Basic realm=\"Android NAS Next\"";
        return Task.CompletedTask;
    }
}
