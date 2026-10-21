using System.Collections.Concurrent;
namespace AndroidNas.Server.Endpoints;

public static class ShareEndpoints
{
    private static readonly ConcurrentDictionary<string, string> _shares = new();
    public static void AddShare(string token, string path) => _shares[token] = path;
    public static string? GetShare(string token) => _shares.TryGetValue(token, out var p) ? p : null;
}
