namespace AndroidNas.Core;

public static class LocalizationManager
{
    private static string _lang = "zh-CN";
    public static void SetLanguage(string lang) { _lang = lang; }

    private static readonly Dictionary<string, Dictionary<string, string>> Dict = new()
    {
        ["zh-CN"] = new() {
            ["app_name"] = "Android NAS",
            ["start"] = "启动服务",
            ["stop"] = "停止服务",
            ["settings"] = "设置",
            ["export"] = "导出配置",
            ["import"] = "导入配置",
            ["https_on"] = "开启 HTTPS (自签)"
        },
        ["en-US"] = new() {
            ["app_name"] = "Android NAS",
            ["start"] = "Start Service",
            ["stop"] = "Stop Service",
            ["settings"] = "Settings",
            ["export"] = "Export Config",
            ["import"] = "Import Config",
            ["https_on"] = "Enable HTTPS (Self-Signed)"
        }
    };

    public static string Get(string key)
    {
        if (Dict.TryGetValue(_lang, out var table) && table.TryGetValue(key, out var val))
            return val;
        return key;
    }
}
