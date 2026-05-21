using Android.Content;

namespace AeroSSH.Services;

/// <summary>Non-secret app preferences (font sizes, defaults, log filters).</summary>
public class AppPreferences
{
    private const string PrefsName = "aerossh_app";
    private const string KeyTerminalFontSize = "term_font_size";
    private const string KeyDefaultPort = "default_port";
    private const string KeyLogFilter = "log_filter";

    public const int DefaultTerminalFontSize = 13;
    public const int MinTerminalFontSize = 9;
    public const int MaxTerminalFontSize = 22;
    public const int DefaultPort = 22;

    private readonly ISharedPreferences _prefs;

    public AppPreferences(Context context)
    {
        _prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
    }

    public int TerminalFontSize
    {
        get => Math.Clamp(_prefs.GetInt(KeyTerminalFontSize, DefaultTerminalFontSize), MinTerminalFontSize, MaxTerminalFontSize);
        set => Put(KeyTerminalFontSize, Math.Clamp(value, MinTerminalFontSize, MaxTerminalFontSize));
    }

    public int DefaultSshPort
    {
        get => _prefs.GetInt(KeyDefaultPort, DefaultPort);
        set => Put(KeyDefaultPort, value);
    }

    public string LogFilter
    {
        get => _prefs.GetString(KeyLogFilter, "all") ?? "all";
        set => Put(KeyLogFilter, value);
    }

    private void Put(string key, int value)
    {
        var e = _prefs.Edit()!;
        e.PutInt(key, value);
        e.Apply();
    }

    private void Put(string key, string value)
    {
        var e = _prefs.Edit()!;
        e.PutString(key, value);
        e.Apply();
    }
}
