using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Activities;
using AeroSSH.Adapters;
using AeroSSH.Models;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;

namespace AeroSSH;

[Activity(
    Label = "@string/app_name",
    Theme = "@style/AppTheme.NoActionBar",
    MainLauncher = true,
    LaunchMode = Android.Content.PM.LaunchMode.SingleTop,
    Exported = true)]
public class MainActivity : AppCompatActivity
{
    private ProfileAdapter _adapter = null!;
    private RecyclerView _list = null!;
    private View _emptyState = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        SetSupportActionBar(FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar));

        _list = FindViewById<RecyclerView>(Resource.Id.profilesList)!;
        _emptyState = FindViewById<View>(Resource.Id.emptyState)!;

        _adapter = new ProfileAdapter(
            onClick: OpenProfile,
            onEdit: EditProfile,
            onDelete: DeleteProfile);
        _list.SetLayoutManager(new LinearLayoutManager(this));
        _list.SetAdapter(_adapter);

        FindViewById<FloatingActionButton>(Resource.Id.fabNew)!.Click += (_, _) => EditProfile(null);
        RefreshList();
    }

    protected override void OnResume()
    {
        base.OnResume();
        RefreshList();
    }

    private void RefreshList()
    {
        var profiles = AeroSshApplication.Instance.Profiles.GetAll();
        _adapter.Submit(profiles);
        _emptyState.Visibility = profiles.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        _list.Visibility = profiles.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
    }

    private void OpenProfile(ServerProfile profile)
    {
        var intent = new Intent(this, typeof(SessionActivity));
        intent.PutExtra(SessionActivity.ExtraProfileId, profile.Id);
        StartActivity(intent);
    }

    private void EditProfile(ServerProfile? profile)
    {
        var intent = new Intent(this, typeof(ProfileEditActivity));
        if (profile != null)
            intent.PutExtra(ProfileEditActivity.ExtraProfileId, profile.Id);
        StartActivity(intent);
    }

    private void DeleteProfile(ServerProfile profile)
    {
        new MaterialAlertDialogBuilder(this)
            .SetTitle(Resource.String.delete_profile_title)!
            .SetMessage(GetString(Resource.String.delete_profile_message, profile.DisplayLabel))!
            .SetPositiveButton(Resource.String.delete, (_, _) =>
            {
                AeroSshApplication.Instance.Profiles.Delete(profile.Id);
                RefreshList();
            })!
            .SetNegativeButton(Resource.String.cancel, (IDialogInterfaceOnClickListener?)null)!
            .Show();
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
    {
        MenuInflater.Inflate(Resource.Menu.menu_main, menu);
        return true;
    }

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        if (item.ItemId == Resource.Id.action_settings)
        {
            StartActivity(new Intent(this, typeof(SettingsActivity)));
            return true;
        }
        return base.OnOptionsItemSelected(item);
    }
}
