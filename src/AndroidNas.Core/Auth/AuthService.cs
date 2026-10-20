namespace AndroidNas.Core;

public interface IAuthStore
{
    string GetUsername();
    string GetPassword();
    bool GetEnabled();
    void SetUsername(string u);
    void SetPassword(string p);
    void SetEnabled(bool e);
}

public static class AuthService
{
    public static IAuthStore? Store { get; set; }
    public static string Username => Store?.GetUsername() ?? "admin";
    public static string Password => Store?.GetPassword() ?? "admin";
    public static bool   Enabled  => Store?.GetEnabled()  ?? true;

    public static void SetPassword(string newPass) => Store?.SetPassword(newPass);
    public static void SetEnabled(bool e)          => Store?.SetEnabled(e);

    public static int VerifyRole(string? authHeader)
    {
        if (!Enabled) return 1;
        if (authHeader == null || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return 0;
        try
        {
            var b64 = authHeader[6..].Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(b64, "^[A-Za-z0-9+/=]+$")) return 0;
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
            if (raw == Username + ":" + Password) return 1;
            if (raw == "guest:guest") return 2;
        }
        catch { return 0; }
        return 0;
    }

    public static bool Verify(string? authHeader) => VerifyRole(authHeader) > 0;
}
