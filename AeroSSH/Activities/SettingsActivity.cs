using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AeroSSH.Services;
using Google.Android.Material.Slider;
using Google.Android.Material.TextField;

namespace AeroSSH.Activities;

[Activity(Label = "@string/settings_title", Theme = "@style/AppTheme")]
public class SettingsActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);
        SupportActionBar?.SetDisplayHomeAsUpEnabled(true);

        InitTheme();
        InitFontSize();
        InitDefaultPort();
        InitHostKeysLink();
        InitVersion();
    }

    private void InitTheme()
    {
        var themeGroup = FindViewById<RadioGroup>(Resource.Id.themeGroup)!;
        var current = ThemeManager.GetStoredMode(this);
        themeGroup.Check(current switch
        {
            ThemeMode.Light => Resource.Id.themeLight,
            ThemeMode.Dark => Resource.Id.themeDark,
            _ => Resource.Id.themeSystem
        });
        themeGroup.CheckedChange += (_, e) =>
        {
            var mode = e.CheckedId switch
            {
                var id when id == Resource.Id.themeLight => ThemeMode.Light,
                var id when id == Resource.Id.themeDark => ThemeMode.Dark,
                _ => ThemeMode.System
            };
            ThemeManager.SetStoredMode(this, mode);
            Recreate();
        };
    }

    private void InitFontSize()
    {
        var slider = FindViewById<Slider>(Resource.Id.fontSizeSlider)!;
        var label = FindViewById<TextView>(Resource.Id.fontSizeValue)!;
        var prefs = AeroSshApplication.Instance.AppPrefs;

        slider.Value = prefs.TerminalFontSize;
        label.Text = GetString(Resource.String.font_size_value, prefs.TerminalFontSize);

#pragma warning disable XAOBS001, CS0618
        slider.AddOnChangeListener(new SliderListener((s, value) =>
        {
            var size = (int)value;
            prefs.TerminalFontSize = size;
            label.Text = GetString(Resource.String.font_size_value, size);
        }));
#pragma warning restore XAOBS001, CS0618
    }

    private void InitDefaultPort()
    {
        var input = FindViewById<TextInputEditText>(Resource.Id.defaultPort)!;
        var prefs = AeroSshApplication.Instance.AppPrefs;
        input.Text = prefs.DefaultSshPort.ToString();
        input.AfterTextChanged += (_, _) =>
        {
            if (int.TryParse(input.Text, out var port) && port is > 0 and <= 65535)
                prefs.DefaultSshPort = port;
        };
    }

    private void InitHostKeysLink()
    {
        FindViewById<Button>(Resource.Id.btnManageHostKeys)!.Click += (_, _) =>
            StartActivity(new Intent(this, typeof(HostKeysActivity)));
    }

    private void InitVersion()
    {
        var info = PackageManager!.GetPackageInfo(PackageName!, PackageInfoFlags.MetaData);
        FindViewById<TextView>(Resource.Id.appVersion)!.Text =
            GetString(Resource.String.version_label, info?.VersionName ?? "?",
                OperatingSystem.IsAndroidVersionAtLeast(28) ? info?.LongVersionCode ?? 0 : info?.VersionCode ?? 0);
    }

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        if (item.ItemId == Android.Resource.Id.Home) { Finish(); return true; }
        return base.OnOptionsItemSelected(item);
    }

#pragma warning disable XAOBS001, CS0618
    private class SliderListener : Java.Lang.Object, Google.Android.Material.Slider.IBaseOnChangeListener
    {
        private readonly Action<Slider, float> _onChange;
        public SliderListener(Action<Slider, float> onChange) => _onChange = onChange;
        public void OnValueChange(Java.Lang.Object slider, float value, bool fromUser)
        {
            if (slider is Slider s) _onChange(s, value);
        }
    }
#pragma warning restore XAOBS001, CS0618
}
