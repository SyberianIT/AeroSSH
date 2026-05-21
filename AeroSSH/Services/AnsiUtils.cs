using System.Text;
using System.Text.RegularExpressions;

namespace AeroSSH.Services;

/// <summary>
/// Minimal ANSI/control sequence sanitizer for terminal output displayed
/// in a plain TextView. Strips CSI/OSC sequences, removes BEL, normalizes
/// line endings, and collapses backspaces.
/// </summary>
public static partial class AnsiUtils
{
    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex CsiRegex();

    [GeneratedRegex(@"\x1B\][^\x07\x1B]*(?:\x07|\x1B\\)")]
    private static partial Regex OscRegex();

    [GeneratedRegex(@"\x1B[@-Z\\-_]")]
    private static partial Regex EscRegex();

    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var stripped = CsiRegex().Replace(input, string.Empty);
        stripped = OscRegex().Replace(stripped, string.Empty);
        stripped = EscRegex().Replace(stripped, string.Empty);
        stripped = stripped.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\a", string.Empty);

        if (stripped.IndexOf('\b') < 0) return stripped;

        var sb = new StringBuilder(stripped.Length);
        foreach (var c in stripped)
        {
            if (c == '\b')
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Length--;
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
