using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Fragment = AndroidX.Fragment.App.Fragment;
using AeroSSH.Fragments;
using AeroSSH.Models;
using AeroSSH.Services;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Dialog;
using Google.Android.Material.Snackbar;

namespace AeroSSH.Activities;

[Activity(Label = "@string/session_title", Theme = "@style/AppTheme.NoActionBar")]
public class SessionActivity : AppCompatActivity
{
    public const string ExtraProfileId = "profile_id";

    private ServerProfile? _profile;
    private SshSession? _session;
    private View _connectingPanel = null!;
    private View _errorPanel = null!;
    private TextView _connectingStatus = null!;
    private TextView _connectingHost = null!;
    private TextView _errorMessage = null!;
    private BottomNavigationView _bottomNav = null!;
    private CancellationTokenSource? _connectCts;

    public string? SessionId => _session?.Id;
    public ISshService? Service => _session != null ? AeroSshApplication.Instance.Sessions.GetService(_session.Id) : null;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_session);
        SetSupportActionBar(FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar));
        SupportActionBar?.SetDisplayHomeAsUpEnabled(true);

        _connectingPanel = FindViewById<View>(Resource.Id.connectingPanel)!;
        _errorPanel = FindViewById<View>(Resource.Id.errorPanel)!;
        _connectingStatus = FindViewById<TextView>(Resource.Id.connectingStatus)!;
        _connectingHost = FindViewById<TextView>(Resource.Id.connectingHost)!;
        _errorMessage = FindViewById<TextView>(Resource.Id.errorMessage)!;
        _bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNav)!;
        _bottomNav.ItemSelected += (_, e) => ShowTab(e.Item.ItemId);

        FindViewById<Button>(Resource.Id.btnRetry)!.Click += async (_, _) => await ConnectAsync();
        FindViewById<Button>(Resource.Id.btnCancelConnect)!.Click += (_, _) => Finish();

        var profileId = Intent?.GetStringExtra(ExtraProfileId);
        if (string.IsNullOrEmpty(profileId)) { Finish(); return; }

        _profile = AeroSshApplication.Instance.Profiles.Get(profileId);
        if (_profile == null) { Finish(); return; }

        Title = _profile.DisplayLabel;
        if (SupportActionBar != null)
            SupportActionBar.Subtitle = $"{_profile.Username}@{_profile.Host}:{_profile.Port}";

        _connectingHost.Text = $"{_profile.Username}@{_profile.Host}:{_profile.Port}";
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        _connectCts?.Cancel();
        _connectCts = new CancellationTokenSource();

        ShowConnecting(GetString(Resource.String.status_resolving)!);

        try
        {
            await Task.Delay(50);
            ShowConnecting(GetString(Resource.String.status_connecting)!);
            _session = await AeroSshApplication.Instance.Sessions.ConnectAsync(_profile!, _connectCts.Token);

            ShowConnecting(GetString(Resource.String.status_ready)!);
            AeroSshApplication.Instance.Profiles.TouchLastUsed(_profile!.Id);
            AeroSshApplication.Instance.Logs.Add(_session.Id, $"Connected to {_profile.DisplayLabel}");
            SshForegroundService.Start(this, _profile.DisplayLabel);

            ShowSession();
        }
        catch (HostKeyVerificationException ex)
        {
            HideAllOverlays();
            ShowHostKeyDialog(ex);
        }
        catch (System.OperationCanceledException)
        {
            Finish();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowConnecting(string status)
    {
        _connectingStatus.Text = status;
        _connectingPanel.Visibility = ViewStates.Visible;
        _errorPanel.Visibility = ViewStates.Gone;
        _bottomNav.Visibility = ViewStates.Gone;
    }

    private void ShowError(string message)
    {
        _errorMessage.Text = message;
        _connectingPanel.Visibility = ViewStates.Gone;
        _errorPanel.Visibility = ViewStates.Visible;
        _bottomNav.Visibility = ViewStates.Gone;
    }

    private void ShowSession()
    {
        _connectingPanel.Visibility = ViewStates.Gone;
        _errorPanel.Visibility = ViewStates.Gone;
        _bottomNav.Visibility = ViewStates.Visible;
        ShowTab(Resource.Id.nav_command);
        _bottomNav.SelectedItemId = Resource.Id.nav_command;
    }

    private void HideAllOverlays()
    {
        _connectingPanel.Visibility = ViewStates.Gone;
        _errorPanel.Visibility = ViewStates.Gone;
    }

    private void ShowHostKeyDialog(HostKeyVerificationException ex)
    {
        var title = ex.IsMismatch
            ? GetString(Resource.String.host_key_mismatch_title)
            : GetString(Resource.String.host_key_unknown_title);
        var message = ex.IsMismatch
            ? GetString(Resource.String.host_key_mismatch_message, ex.Host, ex.ReceivedFingerprint, ex.KnownFingerprint ?? string.Empty)
            : GetString(Resource.String.host_key_unknown_message, ex.Host, ex.ReceivedFingerprint);

        new MaterialAlertDialogBuilder(this)
            .SetTitle(title)!
            .SetMessage(message)!
            .SetPositiveButton(Resource.String.trust_and_connect, async (_, _) =>
            {
                AeroSshApplication.Instance.HostKeys.Trust(ex.Host, ex.Port, ex.ReceivedFingerprint);
                await ConnectAsync();
            })!
            .SetNegativeButton(Resource.String.cancel, (_, _) => Finish())!
            .SetCancelable(false)!
            .Show();
    }

    private void ShowTab(int itemId)
    {
        Fragment fragment = itemId switch
        {
            var id when id == Resource.Id.nav_shell => new ShellFragment(),
            var id when id == Resource.Id.nav_sftp => new SftpFragment(),
            var id when id == Resource.Id.nav_logs => new LogsFragment(),
            _ => new CommandFragment()
        };
        SupportFragmentManager.BeginTransaction()
            .Replace(Resource.Id.fragmentHost, fragment)
            .Commit();
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
    {
        MenuInflater.Inflate(Resource.Menu.menu_session, menu);
        return true;
    }

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        if (item.ItemId == Android.Resource.Id.Home) { Finish(); return true; }
        if (item.ItemId == Resource.Id.action_disconnect) { Finish(); return true; }
        return base.OnOptionsItemSelected(item);
    }

    protected override async void OnDestroy()
    {
        base.OnDestroy();
        _connectCts?.Cancel();
        if (_session != null)
        {
            try
            {
                AeroSshApplication.Instance.Logs.Add(_session.Id, "Disconnected");
                await AeroSshApplication.Instance.Sessions.DisconnectAsync(_session.Id);
            }
            catch { /* shutdown */ }
        }
        SshForegroundService.Stop(this);
    }

    public void Toast(string text) =>
        Snackbar.Make(_bottomNav, text, Snackbar.LengthShort)!.Show();
}
