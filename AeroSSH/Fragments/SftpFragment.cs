using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using AeroSSH.Activities;
using AeroSSH.Adapters;
using AeroSSH.Models;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextField;

namespace AeroSSH.Fragments;

public class SftpFragment : Fragment
{
    private SftpEntryAdapter _adapter = null!;
    private SwipeRefreshLayout _refresh = null!;
    private LinearLayout _breadcrumb = null!;
    private RecyclerView _list = null!;
    private FloatingActionButton _fabUpload = null!;
    private FloatingActionButton _fabMkdir = null!;
    private View _progressContainer = null!;
    private TextView _progressLabel = null!;
    private ProgressBar _progressBar = null!;
    private string _currentPath = "/";
    private SftpEntry? _pendingDownload;

    private ActivityResultLauncher? _pickUploadLauncher;
    private ActivityResultLauncher? _createDownloadLauncher;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var view = inflater.Inflate(Resource.Layout.fragment_sftp, container, false)!;
        _breadcrumb = view.FindViewById<LinearLayout>(Resource.Id.sftpBreadcrumb)!;
        _list = view.FindViewById<RecyclerView>(Resource.Id.sftpList)!;
        _refresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.sftpRefresh)!;
        _fabUpload = view.FindViewById<FloatingActionButton>(Resource.Id.fabUpload)!;
        _fabMkdir = view.FindViewById<FloatingActionButton>(Resource.Id.fabMkdir)!;
        _progressContainer = view.FindViewById<View>(Resource.Id.sftpProgress)!;
        _progressLabel = view.FindViewById<TextView>(Resource.Id.sftpProgressLabel)!;
        _progressBar = view.FindViewById<ProgressBar>(Resource.Id.sftpProgressBar)!;

        _adapter = new SftpEntryAdapter(OnEntryClick, OnEntryLongClick);
        _list.SetLayoutManager(new LinearLayoutManager(Activity));
        _list.SetAdapter(_adapter);
        _refresh.Refresh += async (_, _) => await LoadAsync(_currentPath);
        _fabUpload.Click += (_, _) => _pickUploadLauncher?.Launch("*/*");
        _fabMkdir.Click += (_, _) => PromptMkdir();

        _pickUploadLauncher = RegisterForActivityResult(new ActivityResultContracts.GetContent(), new ResultCallback(OnUploadPicked));
        _createDownloadLauncher = RegisterForActivityResult(new ActivityResultContracts.CreateDocument("*/*"), new ResultCallback(OnDownloadTarget));

        return view;
    }

    public override async void OnResume()
    {
        base.OnResume();
        await LoadAsync(_currentPath);
    }

    private async Task LoadAsync(string path)
    {
        var service = ((SessionActivity)Activity!).Service;
        if (service == null) return;

        _refresh.Refreshing = true;
        try
        {
            var entries = await service.SftpListDirectoryAsync(path, CancellationToken.None);
            _currentPath = path;
            RenderBreadcrumb();
            _adapter.Submit(entries);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally { _refresh.Refreshing = false; }
    }

    private void RenderBreadcrumb()
    {
        _breadcrumb.RemoveAllViews();
        var parts = _currentPath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        AddBreadcrumbSegment("/", "/", root: true);
        var accumulated = "";
        foreach (var part in parts)
        {
            AddBreadcrumbChevron();
            accumulated = accumulated.Length == 0 ? "/" + part : accumulated + "/" + part;
            AddBreadcrumbSegment(part, accumulated, root: false);
        }
    }

    private void AddBreadcrumbSegment(string label, string targetPath, bool root)
    {
        var ctx = Activity!;
        var tv = new TextView(ctx)
        {
            Text = root ? "/" : label,
            Clickable = true,
            Focusable = true
        };
        tv.SetTextAppearance(Android.Resource.Style.TextAppearanceMedium);
        tv.SetTypeface(Android.Graphics.Typeface.Monospace, Android.Graphics.TypefaceStyle.Normal);
        tv.SetPadding(12, 6, 12, 6);
        tv.SetTextSize(Android.Util.ComplexUnitType.Sp, 13);

        if (targetPath == _currentPath)
        {
            tv.SetTypeface(Android.Graphics.Typeface.Monospace, Android.Graphics.TypefaceStyle.Bold);
        }
        else
        {
            tv.Click += async (_, _) => await LoadAsync(targetPath);
        }

        if (targetPath == _currentPath)
            tv.LongClick += (_, e) => { e.Handled = true; PromptGoToPath(); };

        _breadcrumb.AddView(tv);
    }

    private void AddBreadcrumbChevron()
    {
        var icon = new ImageView(Activity!);
        icon.SetImageResource(Resource.Drawable.ic_chevron_right);
        icon.Alpha = 0.5f;
        var size = (int)(16 * Resources!.DisplayMetrics!.Density);
        icon.LayoutParameters = new LinearLayout.LayoutParams(size, size);
        _breadcrumb.AddView(icon);
    }

    private void PromptGoToPath()
    {
        var ctx = Activity!;
        var edit = new TextInputEditText(ctx) { Text = _currentPath };
        edit.SetSingleLine(true);
        var layout = new LinearLayout(ctx) { Orientation = Android.Widget.Orientation.Vertical };
        var pad = (int)(16 * Resources!.DisplayMetrics!.Density);
        layout.SetPadding(pad, 0, pad, 0);
        layout.AddView(edit);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle(Resource.String.go_to_path)!
            .SetView(layout)!
            .SetPositiveButton(Resource.String.ok, async (_, _) =>
            {
                var path = edit.Text?.Trim();
                if (!string.IsNullOrEmpty(path)) await LoadAsync(path);
            })!
            .SetNegativeButton(Resource.String.cancel, (IDialogInterfaceOnClickListener?)null)!
            .Show();
    }

    private void PromptMkdir()
    {
        var ctx = Activity!;
        var edit = new TextInputEditText(ctx);
        edit.SetHint(Resource.String.mkdir_hint);
        var layout = new LinearLayout(ctx) { Orientation = Android.Widget.Orientation.Vertical };
        var pad = (int)(16 * Resources!.DisplayMetrics!.Density);
        layout.SetPadding(pad, 0, pad, 0);
        layout.AddView(edit);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle(Resource.String.mkdir)!
            .SetView(layout)!
            .SetPositiveButton(Resource.String.create, async (_, _) =>
            {
                var name = edit.Text?.Trim();
                if (string.IsNullOrEmpty(name)) return;
                var service = ((SessionActivity)Activity!).Service;
                if (service == null) return;
                var target = Join(_currentPath, name);
                try
                {
                    await service.SftpCreateDirectoryAsync(target, CancellationToken.None);
                    await LoadAsync(_currentPath);
                }
                catch (Exception ex) { ShowError(ex.Message); }
            })!
            .SetNegativeButton(Resource.String.cancel, (IDialogInterfaceOnClickListener?)null)!
            .Show();
    }

    private void PromptRename(SftpEntry entry)
    {
        var ctx = Activity!;
        var edit = new TextInputEditText(ctx) { Text = entry.Name };
        var layout = new LinearLayout(ctx) { Orientation = Android.Widget.Orientation.Vertical };
        var pad = (int)(16 * Resources!.DisplayMetrics!.Density);
        layout.SetPadding(pad, 0, pad, 0);
        layout.AddView(edit);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle(Resource.String.rename)!
            .SetView(layout)!
            .SetPositiveButton(Resource.String.save_profile, async (_, _) =>
            {
                var newName = edit.Text?.Trim();
                if (string.IsNullOrEmpty(newName) || newName == entry.Name) return;
                var service = ((SessionActivity)Activity!).Service;
                if (service == null) return;
                var target = Join(_currentPath, newName);
                try
                {
                    await service.SftpRenameAsync(entry.FullPath, target, CancellationToken.None);
                    await LoadAsync(_currentPath);
                }
                catch (Exception ex) { ShowError(ex.Message); }
            })!
            .SetNegativeButton(Resource.String.cancel, (IDialogInterfaceOnClickListener?)null)!
            .Show();
    }

    private static string Join(string dir, string name)
    {
        if (string.IsNullOrEmpty(dir) || dir == "/") return "/" + name;
        return dir.TrimEnd('/') + "/" + name;
    }

    private static string Parent(string path)
    {
        var idx = path.TrimEnd('/').LastIndexOf('/');
        if (idx <= 0) return "/";
        return path.Substring(0, idx);
    }

    private async void OnEntryClick(SftpEntry entry)
    {
        if (entry.IsDirectory) await LoadAsync(entry.FullPath);
        else
        {
            _pendingDownload = entry;
            _createDownloadLauncher?.Launch(entry.Name);
        }
    }

    private void OnEntryLongClick(SftpEntry entry)
    {
        var ctx = Activity!;
        new MaterialAlertDialogBuilder(ctx)
            .SetTitle(entry.Name)!
            .SetItems(new[] {
                GetString(Resource.String.rename),
                GetString(Resource.String.delete)
            }, (_, args) =>
            {
                if (args.Which == 0) PromptRename(entry);
                else _ = DeleteAsync(entry);
            })!
            .Show();
    }

    private async Task DeleteAsync(SftpEntry entry)
    {
        var service = ((SessionActivity)Activity!).Service;
        if (service == null) return;
        try
        {
            await service.SftpDeleteAsync(entry.FullPath, entry.IsDirectory, CancellationToken.None);
            await LoadAsync(_currentPath);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async void OnUploadPicked(Java.Lang.Object? result)
    {
        if (result is not Android.Net.Uri uri) return;
        var service = ((SessionActivity)Activity!).Service;
        if (service == null) return;
        var name = QueryDisplayName(uri) ?? Guid.NewGuid().ToString("N");
        var size = QuerySize(uri);
        var target = Join(_currentPath, name);

        ShowProgress(GetString(Resource.String.uploading, name)!);
        var progress = new Progress<long>(transferred => UpdateProgress(transferred, size));
        try
        {
            using var stream = Activity!.ContentResolver!.OpenInputStream(uri)!;
            await service.SftpUploadAsync(stream, target, progress, CancellationToken.None);
            HideProgress();
            await LoadAsync(_currentPath);
        }
        catch (Exception ex)
        {
            HideProgress();
            ShowError(ex.Message);
        }
    }

    private async void OnDownloadTarget(Java.Lang.Object? result)
    {
        if (result is not Android.Net.Uri uri || _pendingDownload == null) return;
        var service = ((SessionActivity)Activity!).Service;
        if (service == null) return;
        var entry = _pendingDownload;
        _pendingDownload = null;

        ShowProgress(GetString(Resource.String.downloading, entry.Name)!);
        var progress = new Progress<long>(transferred => UpdateProgress(transferred, entry.Length));
        try
        {
            using var stream = Activity!.ContentResolver!.OpenOutputStream(uri)!;
            await service.SftpDownloadAsync(entry.FullPath, stream, progress, CancellationToken.None);
            HideProgress();
            Snackbar.Make(_list, GetString(Resource.String.downloaded, entry.Name)!, Snackbar.LengthShort)!.Show();
        }
        catch (Exception ex)
        {
            HideProgress();
            ShowError(ex.Message);
        }
    }

    private void ShowProgress(string label)
    {
        _progressContainer.Visibility = ViewStates.Visible;
        _progressLabel.Text = label;
        _progressBar.Progress = 0;
        _progressBar.Indeterminate = true;
    }

    private void UpdateProgress(long transferred, long total)
    {
        Activity?.RunOnUiThread(() =>
        {
            if (total <= 0)
            {
                _progressBar.Indeterminate = true;
                _progressLabel.Text = $"{FormatBytes(transferred)}";
            }
            else
            {
                _progressBar.Indeterminate = false;
                _progressBar.Progress = (int)Math.Clamp(transferred * 100 / total, 0, 100);
                _progressLabel.Text = $"{FormatBytes(transferred)} / {FormatBytes(total)}";
            }
        });
    }

    private void HideProgress() => _progressContainer.Visibility = ViewStates.Gone;

    private void ShowError(string message) => Snackbar.Make(_list, message, Snackbar.LengthLong)!.Show();

    private string? QueryDisplayName(Android.Net.Uri uri)
    {
        try
        {
            using var cursor = Activity!.ContentResolver!.Query(uri, new[] { OpenableColumns.DisplayName }, null, null, null);
            if (cursor != null && cursor.MoveToFirst()) return cursor.GetString(0);
        }
        catch { }
        return null;
    }

    private long QuerySize(Android.Net.Uri uri)
    {
        try
        {
            using var cursor = Activity!.ContentResolver!.Query(uri, new[] { OpenableColumns.Size }, null, null, null);
            if (cursor != null && cursor.MoveToFirst() && !cursor.IsNull(0)) return cursor.GetLong(0);
        }
        catch { }
        return 0;
    }

    private static string FormatBytes(long bytes)
    {
        string[] u = { "B", "KB", "MB", "GB", "TB" };
        double s = bytes;
        var i = 0;
        while (s >= 1024 && i < u.Length - 1) { s /= 1024; i++; }
        return $"{s:0.##} {u[i]}";
    }

    private class ResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        private readonly Action<Java.Lang.Object?> _onResult;
        public ResultCallback(Action<Java.Lang.Object?> onResult) => _onResult = onResult;
        public void OnActivityResult(Java.Lang.Object? result) => _onResult(result);
    }
}
