namespace AndroidNas.Core;

public interface ISettingsStore
{
    string GetString(string key, string def);
    int GetInt(string key, int def);
    bool GetBool(string key, bool def);
    void SetString(string key, string value);
    void SetInt(string key, int value);
    void SetBool(string key, bool value);
}

public static class AppSettings
{
    public static ISettingsStore? Store { get; set; }
    private static string S(string k, string d) => Store?.GetString(k, d) ?? d;
    private static int    I(string k, int d)    => Store?.GetInt(k, d) ?? d;
    private static bool   B(string k, bool d)   => Store?.GetBool(k, d) ?? d;
    private static void SS(string k, string v) => Store?.SetString(k, v);
    private static void SI(string k, int v)    => Store?.SetInt(k, v);
    private static void SB(string k, bool v)   => Store?.SetBool(k, v);

    public static string DeviceAlias    { get => S("device_alias", "Android NAS Next"); set => SS("device_alias", value); }
    public static bool   GlobalMode     { get => B("global_mode", false); set => SB("global_mode", value); }
    public static bool   LowPowerMode   { get => B("low_power_mode", false); set => SB("low_power_mode", value); }
    public static bool   SilentKeepAlive{ get => B("silent_keepalive", true); set => SB("silent_keepalive", value); }

    public static bool WebDavEnabled    { get => B("webdav_enabled", true); set => SB("webdav_enabled", value); }
    public static bool FtpEnabled       { get => B("ftp_enabled", true) && !LowPowerMode; set => SB("ftp_enabled", value); }
    public static bool DlnaEnabled      { get => B("dlna_enabled", true) && !LowPowerMode; set => SB("dlna_enabled", value); }
    public static bool LocalSendEnabled { get => B("localsend_enabled", true) && !LowPowerMode; set => SB("localsend_enabled", value); }
    public static bool StreamEnabled    { get => B("stream_enabled", true) && !LowPowerMode; set => SB("stream_enabled", value); }
    public static bool ClipboardEnabled { get => B("clipboard_enabled", true) && !LowPowerMode; set => SB("clipboard_enabled", value); }

    public static int WebPort    { get => I("web_port", 8080); set => SI("web_port", value); }
    public static int WebDavPort { get => I("webdav_port", 8081); set => SI("webdav_port", value); }
    public static int FtpPort    { get => I("ftp_port", 2121); set => SI("ftp_port", value); }

    public static string LocalSendPin { get => S("ls_pin", "1234"); set => SS("ls_pin", value); }

    public static string RootPath
    {
        get
        {
            if (GlobalMode) return "/";
#if ANDROID
            return Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "/storage/emulated/0";
#else
            return Path.GetTempPath();
#endif
        }
    }

    public static string Fingerprint
    {
        get
        {
            var fp = S("fingerprint", "");
            if (string.IsNullOrEmpty(fp)) { fp = Guid.NewGuid().ToString(); SS("fingerprint", fp); }
            return fp;
        }
    }
}
