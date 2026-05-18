using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Activities;
using AeroSSH.Adapters;
using AeroSSH.Models;
using Google.Android.Material.Snackbar;
using Java.IO;

namespace AeroSSH.Fragments;

public class LogsFragment : Fragment
{
    private LogAdapter _adapter = null!;
    private RecyclerView _list = null!;
    private Button _exportJson = null!, _exportText = null!, _clear = null!;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var view = inflater.Inflate(Resource.Layout.fragment_logs, container, false)!;
        _list = view.FindViewById<RecyclerView>(Resource.Id.logList)!;
        _exportJson = view.FindViewById<Button>(Resource.Id.btnExportJson)!;
        _exportText = view.FindViewById<Button>(Resource.Id.btnExportText)!;
        _clear = view.FindViewById<Button>(Resource.Id.btnClearLogs)!;

        _adapter = new LogAdapter();
        _list.SetLayoutManager(new LinearLayoutManager(Activity) { StackFromEnd = true });
        _list.SetAdapter(_adapter);

        _exportJson.Click += async (_, _) => await ExportAsync(json: true);
        _exportText.Click += async (_, _) => await ExportAsync(json: false);
        _clear.Click += (_, _) =>
        {
            var id = ((SessionActivity)Activity!).SessionId;
            if (id != null) AeroSshApplication.Instance.Logs.Clear(id);
            Refresh();
        };

        AeroSshApplication.Instance.Logs.EntryAdded += OnEntryAdded;
        return view;
    }

    public override void OnDestroyView()
    {
        AeroSshApplication.Instance.Logs.EntryAdded -= OnEntryAdded;
        base.OnDestroyView();
    }

    public override void OnResume()
    {
        base.OnResume();
        Refresh();
    }

    private void OnEntryAdded(object? sender, LogEntry entry)
    {
        var id = ((SessionActivity?)Activity)?.SessionId;
        if (id != entry.SessionId) return;
        Activity?.RunOnUiThread(Refresh);
    }

    private void Refresh()
    {
        var id = ((SessionActivity)Activity!).SessionId;
        if (id == null) return;
        var logs = AeroSshApplication.Instance.Logs.Get(id);
        _adapter.Submit(logs);
        if (logs.Count > 0) _list.ScrollToPosition(logs.Count - 1);
    }

    private async Task ExportAsync(bool json)
    {
        var id = ((SessionActivity)Activity!).SessionId;
        if (id == null) return;
        try
        {
            var path = json
                ? await AeroSshApplication.Instance.Logs.ExportAsync(id)
                : await AeroSshApplication.Instance.Logs.ExportAsTextAsync(id);
            ShareFile(path);
        }
        catch (Exception ex)
        {
            Snackbar.Make(_list, ex.Message, Snackbar.LengthLong)!.Show();
        }
    }

    private void ShareFile(string path)
    {
        var file = new File(path);
        var uri = FileProvider.GetUriForFile(Activity!, $"{Activity!.PackageName}.fileprovider", file);
        var intent = new Intent(Intent.ActionSend);
        intent.SetType(path.EndsWith(".json") ? "application/json" : "text/plain");
        intent.PutExtra(Intent.ExtraStream, uri);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        StartActivity(Intent.CreateChooser(intent, GetString(Resource.String.share_log)));
    }
}
