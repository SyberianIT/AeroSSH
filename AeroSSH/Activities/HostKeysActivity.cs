using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Adapters;
using Google.Android.Material.Dialog;

namespace AeroSSH.Activities;

[Activity(Label = "@string/manage_host_keys", Theme = "@style/AppTheme")]
public class HostKeysActivity : AppCompatActivity
{
    private HostKeyAdapter _adapter = null!;
    private View _emptyState = null!;
    private RecyclerView _list = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_host_keys);
        SupportActionBar?.SetDisplayHomeAsUpEnabled(true);

        _list = FindViewById<RecyclerView>(Resource.Id.hostKeysList)!;
        _emptyState = FindViewById<View>(Resource.Id.hostKeysEmpty)!;

        _adapter = new HostKeyAdapter(OnForget);
        _list.SetLayoutManager(new LinearLayoutManager(this));
        _list.SetAdapter(_adapter);
        Refresh();
    }

    private void Refresh()
    {
        var entries = AeroSshApplication.Instance.HostKeys.All().ToList();
        _adapter.Submit(entries);
        _emptyState.Visibility = entries.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        _list.Visibility = entries.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
    }

    private void OnForget(string host, int port)
    {
        new MaterialAlertDialogBuilder(this)
            .SetTitle(Resource.String.forget_key_title)!
            .SetMessage(GetString(Resource.String.forget_key_message, host, port))!
            .SetPositiveButton(Resource.String.forget, (_, _) =>
            {
                AeroSshApplication.Instance.HostKeys.Forget(host, port);
                Refresh();
            })!
            .SetNegativeButton(Resource.String.cancel, (IDialogInterfaceOnClickListener?)null)!
            .Show();
    }

    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        if (item.ItemId == Android.Resource.Id.Home) { Finish(); return true; }
        return base.OnOptionsItemSelected(item);
    }
}
