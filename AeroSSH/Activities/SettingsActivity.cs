using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AeroSSH.Services;

namespace AeroSSH.Activities;

[Activity(Label = "@string/settings_title", Theme = "@style/AppTheme")]
public class SettingsActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);
        SupportActionBar?.SetDisplayHomeAsUpEnabled(true);

        var themeGroup = FindViewById<RadioGroup>(Resource.Id.themeGroup)!;
        var current = ThemeManager.GetStoredMode(this);
        var checkId = current switch
        {
            ThemeMode.Light => Resource.Id.themeLight,
            ThemeMode.Dark => Resource.Id.themeDark,
            _ => Resource.Id.themeSystem
        };
        themeGroup.Check(checkId);

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

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        if (item.ItemId == Android.Resource.Id.Home) { Finish(); return true; }
        return base.OnOptionsItemSelected(item);
    }
}
