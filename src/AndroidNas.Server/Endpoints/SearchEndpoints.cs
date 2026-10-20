using System.Security;
using AndroidNas.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AndroidNas.Server.Endpoints;

public static class SearchEndpoints
{
    private const int MaxResults = 200;
    private const int MaxDepth = 12;

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", (string? q, IFileRootProvider root) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.Ok(Array.Empty<object>());

            string rootFull;
            try { rootFull = root.Resolve("/"); }
            catch (SecurityException) { return Results.BadRequest("Invalid root"); }

            var needle = q.ToLowerInvariant();
            var results = new List<FileEntryDto>();

            void Walk(string dir, int depth)
            {
                if (depth > MaxDepth || results.Count >= MaxResults) return;
                IEnumerable<FileSystemInfo> children;
                try { children = new DirectoryInfo(dir).EnumerateFileSystemInfos(); }
                catch { return; }

                foreach (var e in children)
                {
                    if (results.Count >= MaxResults) return;
                    if (e.Name.ToLowerInvariant().Contains(needle))
                    {
                        var isDir = e is DirectoryInfo;
                        long size = 0;
                        if (!isDir && e is FileInfo fi) try { size = fi.Length; } catch { }
                        results.Add(new FileEntryDto(e.Name, isDir, size,
                            !isDir && MediaTypeHelper.IsTxt(e.Name),
                            !isDir && MediaTypeHelper.IsMedia(e.Name),
                            !isDir && MediaTypeHelper.IsZip(e.Name)));
                    }
                    if (e is DirectoryInfo sub)
                        Walk(sub.FullName, depth + 1);
                }
            }

            Walk(rootFull, 0);
            return Results.Ok(results);
        });

        return app;
    }
}
