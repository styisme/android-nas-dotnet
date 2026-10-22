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

    /// <summary>
    /// 初始化日志目录。若未指定，则尝试使用默认路径。
    /// </summary>
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
                // 默认尝试放在 Download 目录
                var baseDir = AppSettings.RootPath;
                _logDir = Path.Combine(baseDir, "Download", "AndroidNAS", "Logs");
            }
            
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
        }
        catch
        {
            // 如果因为权限（如 Android 10+ 沙盒或未授权）无法创建，则降级到 AppContext.BaseDirectory
            _logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            try { if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir); } catch { _fileLogEnabled = false; }
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
                catch { _fileLogEnabled = false; } // 发生异常则永久关闭文件日志，避免死循环
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
}
