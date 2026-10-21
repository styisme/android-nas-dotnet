namespace AndroidNas.Core;

public static class LocalizationManager
{
    private static string _lang = "zh-CN";
    public static event Action? LanguageChanged;

    public static string CurrentLanguage => _lang;

    public static void SetLanguage(string lang)
    {
        if (_lang == lang) return;
        _lang = lang;
        LanguageChanged?.Invoke();
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Dict = new()
    {
        ["zh-CN"] = new() {
            ["app_name"] = "Android NAS",
            ["title_home"] = "首页",
            ["service_status"] = "文件服务",
            ["running"] = "运行中",
            ["stopped"] = "已停止",
            ["start"] = "启动",
            ["stop"] = "停止",
            ["language"] = "界面语言",
            ["settings"] = "设置",
            ["save_restart"] = "保存并重启服务",
            
            // 电脑端词条
            ["choose_lang"] = "请选择语言 (1. 中文  2. English) [默认 1]: ",
            ["srv_started"] = "Web 服务已启动在端口: {0}",
            ["srv_https"] = "HTTPS 已启动在端口: {0}",
            ["srv_opening"] = "正在自动打开浏览器管理界面...",
            ["srv_open_manual"] = "请手动在浏览器打开: {0}",
            ["srv_stop_hint"] = "按 'q' 键或 Ctrl+C 停止服务。",
            
            ["cli_welcome"] = "=== Android NAS 桌面客户端 ===",
            ["cli_ip"] = "请输入 NAS IP (例如 192.168.1.100): ",
            ["cli_port"] = "端口 (默认 8080): ",
            ["cli_user"] = "账号 (默认 admin): ",
            ["cli_pass"] = "密码 (默认 admin): ",
            ["cli_conn_success"] = "✅ 连接成功！输入 'help' 查看命令。\n",
            ["cli_conn_fail"] = "❌ 无法连接到服务器。",
            ["cli_help"] = "ls - 查看目录\ncd <目录> - 切换目录\nget <文件名> - 下载文件\nexit - 退出",
            ["cli_unknown"] = "未知命令，输入 'help' 查看帮助",
            ["cli_downloaded"] = "已下载到本地: {0}",
            ["cli_dir_error"] = "目录不存在"
        },
        ["en-US"] = new() {
            ["app_name"] = "Android NAS",
            ["title_home"] = "Home",
            ["service_status"] = "File Service",
            ["running"] = "Running",
            ["stopped"] = "Stopped",
            ["start"] = "Start",
            ["stop"] = "Stop",
            ["language"] = "Language",
            ["settings"] = "Settings",
            ["save_restart"] = "Save & Restart",
            
            // 电脑端词条
            ["choose_lang"] = "Choose language (1. 中文  2. English) [Default 1]: ",
            ["srv_started"] = "Web server started on port: {0}",
            ["srv_https"] = "HTTPS started on port: {0}",
            ["srv_opening"] = "Opening browser automatically...",
            ["srv_open_manual"] = "Please open manually: {0}",
            ["srv_stop_hint"] = "Press 'q' or Ctrl+C to stop the server.",
            
            ["cli_welcome"] = "=== Android NAS Desktop Client ===",
            ["cli_ip"] = "Enter NAS IP (e.g., 192.168.1.100): ",
            ["cli_port"] = "Port (default 8080): ",
            ["cli_user"] = "Username (default admin): ",
            ["cli_pass"] = "Password (default admin): ",
            ["cli_conn_success"] = "✅ Connected! Type 'help' for commands.\n",
            ["cli_conn_fail"] = "❌ Failed to connect to server.",
            ["cli_help"] = "ls - List files\ncd <dir> - Change directory\nget <file> - Download file\nexit - Quit",
            ["cli_unknown"] = "Unknown command, type 'help' for assistance",
            ["cli_downloaded"] = "Downloaded to local: {0}",
            ["cli_dir_error"] = "Directory does not exist"
        }
    };

    public static string Get(string key)
    {
        if (Dict.TryGetValue(_lang, out var table) && table.TryGetValue(key, out var val))
            return val;
        return key;
    }
}
