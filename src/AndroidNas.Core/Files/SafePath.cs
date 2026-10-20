using System.Security;

namespace AndroidNas.Core;

public static class SafePath
{
    public static string Resolve(string rootPath, string? requested)
    {
        var req = string.IsNullOrWhiteSpace(requested) ? "/" : requested.Replace('\\', '/');
        if (!req.StartsWith('/')) req = "/" + req;

        foreach (var seg in req.Split('/', StringSplitOptions.RemoveEmptyEntries))
            if (seg == "..") throw new SecurityException("Path traversal detected.");

        var rootFull = Path.GetFullPath(rootPath).TrimEnd('/');
        var combined = req == "/"
            ? rootFull
            : Path.GetFullPath(Path.Combine(rootFull, req.TrimStart('/')));

        if (!combined.Equals(rootFull, StringComparison.Ordinal) &&
            !combined.StartsWith(rootFull + "/", StringComparison.Ordinal))
            throw new SecurityException("Path outside of root.");

        return combined;
    }

    public static string ToVirtual(string rootPath, string physical)
    {
        var rootFull = Path.GetFullPath(rootPath).TrimEnd('/');
        if (physical.Equals(rootFull, StringComparison.Ordinal)) return "/";
        if (physical.StartsWith(rootFull + "/", StringComparison.Ordinal))
            return "/" + physical[(rootFull.Length + 1)..].Replace('\\', '/');
        return physical;
    }
}
