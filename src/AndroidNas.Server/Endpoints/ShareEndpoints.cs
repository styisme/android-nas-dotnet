using System.Collections.Concurrent;
using AndroidNas.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AndroidNas.Server.Endpoints;

public static class ShareEndpoints
{
    private static readonly ConcurrentDictionary<string, string> _shares = new();

    public static IEndpointRouteBuilder MapShareEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/share", async (HttpRequest req) =>
        {
            var form = await req.ReadFormAsync();
            var p = form["path"].ToString();
            if (string.IsNullOrEmpty(p)) return Results.BadRequest();
            var token = Guid.NewGuid().ToString("N")[..8];
            _shares[token] = p;
            return Results.Text(token);
        });

        app.MapGet("/share/{token}", (string token, IFileRootProvider root) =>
        {
            if (!_shares.TryGetValue(token, out var p)) return Results.NotFound();
            string full;
            try { full = root.Resolve(p); }
            catch { return Results.NotFound(); }
            if (!File.Exists(full)) return Results.NotFound();
            return Results.File(full, "application/octet-stream", Path.GetFileName(full));
        });

        return app;
    }
}
