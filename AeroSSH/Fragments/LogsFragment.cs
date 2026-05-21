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
using Google.Android.Material.Chip;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextField;
using Java.IO;

namespace AeroSSH.Fragments;

public class LogsFragment : Fragment
{
    private LogAdapter _adapter = null!;
    private RecyclerView _list = null!;
    private Button _exportJson = null!, _exportText = null!, _clear = null!;
    private ChipGroup _chips = null!;
    private TextInputEditText _search = null!;

    private string _filter = "all";
    private string _query = string.Empty;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var view = inflater.Inflate(Resource.Layout.fragment_logs, container, false)!;
        _list = view.FindViewById<RecyclerView>(Resource.Id.logList)!;
        _exportJson = view.FindViewById<Button>(Resource.Id.btnExportJson)!;
        _exportText = view.FindViewById<Button>(Resource.Id.btnExportText)!;
        _clear = view.FindViewById<Button>(Resource.Id.btnClearLogs)!;
        _chips = view.FindViewById<ChipGroup>(Resource.Id.logFilterChips)!;
        _search = view.FindViewById<TextInputEditText>(Resource.Id.logSearch)!;

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

        _filter = AeroSshApplication.Instance.AppPrefs.LogFilter;
        ApplyFilterToChips();
        _chips.CheckedChange += (_, e) =>
        {
            _filter = e.CheckedIds.Count > 0 ? ChipIdToFilter(e.CheckedIds[0]) : "all";
            AeroSshApplication.Instance.AppPrefs.LogFilter = _filter;
            Refresh();
        };

        _search.TextChanged += (_, e) =>
        {
            _query = (_search.Text ?? string.Empty).Trim();
            Refresh();
        };

        AeroSshApplication.Instance.Logs.EntryAdded += OnEntryAdded;
        return view;
    }

    private void ApplyFilterToChips()
    {
        var id = _filter switch
        {
            "error" => Resource.Id.chipErrors,
            "stdout" => Resource.Id.chipStdout,
            "stderr" => Resource.Id.chipStderr,
            "system" => Resource.Id.chipSystem,
            "command" => Resource.Id.chipCommand,
            _ => Resource.Id.chipAll
        };
        _chips.Check(id);
    }

    private static string ChipIdToFilter(int id) =>
        id == Resource.Id.chipErrors ? "error" :
        id == Resource.Id.chipStdout ? "stdout" :
        id == Resource.Id.chipStderr ? "stderr" :
        id == Resource.Id.chipSystem ? "system" :
        id == Resource.Id.chipCommand ? "command" : "all";

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
        var filtered = logs.Where(MatchesFilter).ToList();
        _adapter.Submit(filtered);
        if (filtered.Count > 0) _list.ScrollToPosition(filtered.Count - 1);
    }

    private bool MatchesFilter(LogEntry e)
    {
        var passesFilter = _filter switch
        {
            "error" => e.Level == LogLevel.Error || e.Source == LogSource.Stderr,
            "stdout" => e.Source == LogSource.Stdout,
            "stderr" => e.Source == LogSource.Stderr,
            "system" => e.Source == LogSource.System,
            "command" => e.Source == LogSource.Command,
            _ => true
        };
        if (!passesFilter) return false;
        if (_query.Length == 0) return true;
        return e.Message.Contains(_query, StringComparison.OrdinalIgnoreCase);
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
