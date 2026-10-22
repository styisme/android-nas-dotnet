using System.Text;

namespace AndroidNas.Core;

public static class NasLog
{
    private const int MaxLogs = 200;
    private static readonly List<string> _logs = new();
    private static readonly object _lock = new();
    public static event Action? Changed;

    private static string? _logDir;
    private static bool _fileLogEnabled = true;

    public static void Init(string? customLogDir = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(customLogDir))
            {
                _logDir = customLogDir;
            }
            else
            {
#if ANDROID
                // 在 Android 上，绝对优先使用 AppDataDirectory（沙盒，不需要任何权限）
                _logDir = Path.Combine(FileSystem.AppDataDirectory, "Logs");
#else
                _logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
#endif
            }
            
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
        }
        catch
        {
            _fileLogEnabled = false;
        }
    }

    public static void Add(string ip, string action)
    {
        var time = DateTime.Now.ToString("MM-dd HH:mm:ss");
        var logLine = $"[{time}] [{ip}] {action}";

        lock (_lock)
        {
            _logs.Insert(0, logLine);
            if (_logs.Count > MaxLogs) _logs.RemoveAt(_logs.Count - 1);

            if (_fileLogEnabled && !string.IsNullOrEmpty(_logDir))
            {
                try
                {
                    var logFile = Path.Combine(_logDir, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                    File.AppendAllText(logFile, logLine + Environment.NewLine, Encoding.UTF8);
                }
                catch { _fileLogEnabled = false; }
            }
        }
        Changed?.Invoke();
    }

    public static void Crash(Exception ex)
    {
        Add("FATAL", $"未处理的致命异常:\n{ex.Message}\n\n堆栈:\n{ex.StackTrace}");
    }

    public static string[] Snapshot() { lock (_lock) return _logs.ToArray(); }
    public static string GetAllAsString() => string.Join("\n\n", Snapshot());
    
    public static string GetLogFilePath()
    {
        if (string.IsNullOrEmpty(_logDir)) return "未初始化";
        return _logDir;
    }
}
