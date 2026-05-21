using Android.App;
using Android.Runtime;
using AeroSSH.Services;

namespace AeroSSH;

[Application(AllowBackup = false)]
public class AeroSshApplication : Application
{
    public static AeroSshApplication Instance { get; private set; } = null!;

    public SecurePreferences SecurePrefs { get; private set; } = null!;
    public AppPreferences AppPrefs { get; private set; } = null!;
    public ProfileStore Profiles { get; private set; } = null!;
    public HostKeyStore HostKeys { get; private set; } = null!;
    public CommandHistoryStore History { get; private set; } = null!;
    public SessionManager Sessions { get; private set; } = null!;
    public LogService Logs { get; private set; } = null!;

    public AeroSshApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }

    public override void OnCreate()
    {
        base.OnCreate();
        Instance = this;

        ThemeManager.ApplyStoredTheme(this);

        SecurePrefs = new SecurePreferences(this);
        AppPrefs = new AppPreferences(this);
        Profiles = new ProfileStore(SecurePrefs);
        HostKeys = new HostKeyStore(SecurePrefs);
        History = new CommandHistoryStore(SecurePrefs);
        Logs = new LogService(this);
        Sessions = new SessionManager(HostKeys);
    }
}
