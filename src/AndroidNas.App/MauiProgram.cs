using AndroidNas.Core;
#if ANDROID
using AndroidNas.App.Platforms.Android;
#endif

namespace AndroidNas.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if ANDROID
        var store = new MauiSettingsStore();
        AppSettings.Store = store;
        AuthService.Store = store;
#endif
        return builder.Build();
    }
}
