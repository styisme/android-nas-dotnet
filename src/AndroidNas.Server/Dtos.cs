namespace AndroidNas.Server;

public sealed record FileEntryDto(
    string Name, bool Dir, long Size, bool IsTxt, bool IsMedia, bool IsZip);

public sealed record ListResponse(string Path, IReadOnlyList<FileEntryDto> Files);

public static class MediaTypeHelper
{
    private static readonly HashSet<string> TxtExt = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".md", ".json", ".java", ".html", ".xml", ".js", ".css", ".py", ".log", ".ini", ".cs" };
    private static readonly HashSet<string> MediaExt = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mp3", ".mkv", ".webm", ".wav", ".flac" };

    public static bool IsTxt(string name)   => TxtExt.Contains(Path.GetExtension(name));
    public static bool IsMedia(string name) => MediaExt.Contains(Path.GetExtension(name));
    public static bool IsZip(string name)   => string.Equals(Path.GetExtension(name), ".zip", StringComparison.OrdinalIgnoreCase);
}
