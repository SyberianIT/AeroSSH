using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.AppCompat.App;
using AeroSSH.Models;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextField;

namespace AeroSSH.Activities;

[Activity(Label = "@string/profile_edit_title", Theme = "@style/AppTheme")]
public class ProfileEditActivity : AppCompatActivity
{
    public const string ExtraProfileId = "profile_id";

    private TextInputEditText _name = null!, _host = null!, _port = null!, _user = null!, _password = null!, _passphrase = null!;
    private RadioGroup _authGroup = null!;
    private TextView _keyStatus = null!;
    private Button _pickKey = null!, _clearKey = null!, _save = null!;
    private byte[]? _keyBytes;

    private ActivityResultLauncher? _pickKeyLauncher;
    private ServerProfile? _editing;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_profile_edit);
        SupportActionBar?.SetDisplayHomeAsUpEnabled(true);

        _name = FindViewById<TextInputEditText>(Resource.Id.editName)!;
        _host = FindViewById<TextInputEditText>(Resource.Id.editHost)!;
        _port = FindViewById<TextInputEditText>(Resource.Id.editPort)!;
        _user = FindViewById<TextInputEditText>(Resource.Id.editUser)!;
        _password = FindViewById<TextInputEditText>(Resource.Id.editPassword)!;
        _passphrase = FindViewById<TextInputEditText>(Resource.Id.editPassphrase)!;
        _authGroup = FindViewById<RadioGroup>(Resource.Id.authGroup)!;
        _keyStatus = FindViewById<TextView>(Resource.Id.keyStatus)!;
        _pickKey = FindViewById<Button>(Resource.Id.btnPickKey)!;
        _clearKey = FindViewById<Button>(Resource.Id.btnClearKey)!;
        _save = FindViewById<Button>(Resource.Id.btnSaveProfile)!;

        _pickKeyLauncher = RegisterForActivityResult(new ActivityResultContracts.GetContent(), new ActivityResultCallback(OnKeyPicked));

        _authGroup.CheckedChange += (_, e) => UpdateAuthVisibility();
        _pickKey.Click += (_, _) => _pickKeyLauncher?.Launch("*/*");
        _clearKey.Click += (_, _) => { _keyBytes = null; _keyStatus.Text = GetString(Resource.String.key_none); };
        _save.Click += (_, _) => SaveProfile();

        var editId = Intent?.GetStringExtra(ExtraProfileId);
        if (!string.IsNullOrEmpty(editId))
        {
            _editing = AeroSshApplication.Instance.Profiles.Get(editId);
            if (_editing != null) FillFromProfile(_editing);
        }
        else
        {
            _port.Text = "22";
            _authGroup.Check(Resource.Id.radioPassword);
        }

        UpdateAuthVisibility();
    }

    private void FillFromProfile(ServerProfile p)
    {
        _name.Text = p.Name;
        _host.Text = p.Host;
        _port.Text = p.Port.ToString();
        _user.Text = p.Username;
        _password.Text = p.Password;
        _passphrase.Text = p.KeyPassphrase;
        _keyBytes = p.PrivateKey;
        _keyStatus.Text = _keyBytes is { Length: > 0 }
            ? GetString(Resource.String.key_loaded, _keyBytes.Length)
            : GetString(Resource.String.key_none);
        _authGroup.Check(p.AuthMethod == AuthMethod.PrivateKey ? Resource.Id.radioKey : Resource.Id.radioPassword);
    }

    private void UpdateAuthVisibility()
    {
        var key = _authGroup.CheckedRadioButtonId == Resource.Id.radioKey;
        FindViewById<View>(Resource.Id.passwordLayout)!.Visibility = key ? ViewStates.Gone : ViewStates.Visible;
        FindViewById<View>(Resource.Id.keyLayout)!.Visibility = key ? ViewStates.Visible : ViewStates.Gone;
    }

    private void OnKeyPicked(Java.Lang.Object? result)
    {
        if (result is not Android.Net.Uri uri) return;
        try
        {
            using var stream = ContentResolver!.OpenInputStream(uri)!;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            _keyBytes = ms.ToArray();
            _keyStatus.Text = GetString(Resource.String.key_loaded, _keyBytes.Length);
        }
        catch (Exception ex)
        {
            Snackbar.Make(_keyStatus, ex.Message, Snackbar.LengthLong)!.Show();
        }
    }

    private void SaveProfile()
    {
        var host = _host.Text?.Trim();
        var user = _user.Text?.Trim();

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user))
        {
            Snackbar.Make(_save, Resource.String.error_required_fields, Snackbar.LengthLong)!.Show();
            return;
        }

        if (!int.TryParse(_port.Text, out var port) || port is <= 0 or > 65535)
        {
            Snackbar.Make(_save, Resource.String.error_invalid_port, Snackbar.LengthLong)!.Show();
            return;
        }

        var authMethod = _authGroup.CheckedRadioButtonId == Resource.Id.radioKey
            ? AuthMethod.PrivateKey
            : AuthMethod.Password;

        var profile = _editing ?? new ServerProfile();
        profile.Name = _name.Text?.Trim() ?? string.Empty;
        profile.Host = host;
        profile.Port = port;
        profile.Username = user;
        profile.AuthMethod = authMethod;
        profile.Password = authMethod == AuthMethod.Password ? _password.Text : null;
        profile.PrivateKey = authMethod == AuthMethod.PrivateKey ? _keyBytes : null;
        profile.KeyPassphrase = authMethod == AuthMethod.PrivateKey ? _passphrase.Text : null;

        AeroSshApplication.Instance.Profiles.Save(profile);
        SetResult(Result.Ok);
        Finish();
    }

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        if (item.ItemId == Android.Resource.Id.Home) { Finish(); return true; }
        return base.OnOptionsItemSelected(item);
    }

    private class ActivityResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        private readonly Action<Java.Lang.Object?> _onResult;
        public ActivityResultCallback(Action<Java.Lang.Object?> onResult) => _onResult = onResult;
        public void OnActivityResult(Java.Lang.Object? result) => _onResult(result);
    }
}
