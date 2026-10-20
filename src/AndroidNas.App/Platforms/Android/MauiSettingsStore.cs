using AndroidNas.Core;
using Microsoft.Maui.Storage;

namespace AndroidNas.App.Platforms.Android;

public class MauiSettingsStore : ISettingsStore, IAuthStore
{
    private static IPreferences P => Preferences.Default;

    public string GetString(string key, string def) => P.Get(key, def);
    public int    GetInt(string key, int def)       => P.Get(key, def);
    public bool   GetBool(string key, bool def)     => P.Get(key, def);
    public void   SetString(string key, string v)   => P.Set(key, v);
    public void   SetInt(string key, int v)         => P.Set(key, v);
    public void   SetBool(string key, bool v)       => P.Set(key, v);

    public string GetUsername() => P.Get("auth_user", "admin");
    public string GetPassword() => P.Get("auth_pass", "admin");
    public bool   GetEnabled()  => P.Get("auth_enabled", true);
    public void   SetUsername(string u) => P.Set("auth_user", u);
    public void   SetPassword(string p) => P.Set("auth_pass", p);
    public void   SetEnabled(bool e)    => P.Set("auth_enabled", e);
}
