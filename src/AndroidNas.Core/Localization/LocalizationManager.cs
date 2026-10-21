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
            ["save_restart"] = "保存并重启服务"
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
            ["save_restart"] = "Save & Restart"
        }
    };

    public static string Get(string key)
    {
        if (Dict.TryGetValue(_lang, out var table) && table.TryGetValue(key, out var val))
            return val;
        return key;
    }
}
