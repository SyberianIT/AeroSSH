using Android.Content;
using Android.Preferences;
using AndroidX.AppCompat.App;

namespace AeroSSH.Services;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public static class ThemeManager
{
    private const string PrefName = "aerossh_prefs";
    private const string KeyTheme = "theme_mode";

    public static ThemeMode GetStoredMode(Context context)
    {
        var prefs = context.GetSharedPreferences(PrefName, FileCreationMode.Private)!;
        return (ThemeMode)prefs.GetInt(KeyTheme, (int)ThemeMode.System);
    }

    public static void SetStoredMode(Context context, ThemeMode mode)
    {
        var prefs = context.GetSharedPreferences(PrefName, FileCreationMode.Private)!;
        var editor = prefs.Edit()!;
        editor.PutInt(KeyTheme, (int)mode);
        editor.Apply();
        Apply(mode);
    }

    public static void ApplyStoredTheme(Context context) => Apply(GetStoredMode(context));

    private static void Apply(ThemeMode mode)
    {
        AppCompatDelegate.DefaultNightMode = mode switch
        {
            ThemeMode.Light => AppCompatDelegate.ModeNightNo,
            ThemeMode.Dark => AppCompatDelegate.ModeNightYes,
            _ => AppCompatDelegate.ModeNightFollowSystem
        };
    }
}
