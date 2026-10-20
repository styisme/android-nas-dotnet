namespace AndroidNas.Core;

public static class NasLog
{
    private const int MaxLogs = 200;
    private static readonly List<string> _logs = new();
    private static readonly object _lock = new();
    public static event Action? Changed;

    public static void Add(string ip, string action)
    {
        lock (_lock)
        {
            _logs.Insert(0, $"[{DateTime.Now:MM-dd HH:mm:ss}] {ip} - {action}");
            if (_logs.Count > MaxLogs) _logs.RemoveAt(_logs.Count - 1);
        }
        Changed?.Invoke();
    }

    public static string[] Snapshot() { lock (_lock) return _logs.ToArray(); }
    public static string GetAllAsString() => string.Join("\n\n", Snapshot());
}
