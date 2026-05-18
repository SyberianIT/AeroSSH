using Android.App;
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

namespace AeroSSH.Fragments;

public class SftpFragment : Fragment
{
    private SftpEntryAdapter _adapter = null!;
    private SwipeRefreshLayout _refresh = null!;
    private TextView _pathView = null!;
    private RecyclerView _list = null!;
    private FloatingActionButton _fabUpload = null!;
    private string _currentPath = ".";
    private SftpEntry? _pendingDownload;

    private ActivityResultLauncher? _pickUploadLauncher;
    private ActivityResultLauncher? _createDownloadLauncher;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var view = inflater.Inflate(Resource.Layout.fragment_sftp, container, false)!;
        _pathView = view.FindViewById<TextView>(Resource.Id.sftpPath)!;
        _list = view.FindViewById<RecyclerView>(Resource.Id.sftpList)!;
        _refresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.sftpRefresh)!;
        _fabUpload = view.FindViewById<FloatingActionButton>(Resource.Id.fabUpload)!;

        _adapter = new SftpEntryAdapter(OnEntryClick, OnEntryLongClick);
        _list.SetLayoutManager(new LinearLayoutManager(Activity));
        _list.SetAdapter(_adapter);
        _refresh.Refresh += async (_, _) => await LoadAsync(_currentPath);
        _fabUpload.Click += (_, _) => _pickUploadLauncher?.Launch("*/*");

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
            _pathView.Text = path;
            var withParent = new List<SftpEntry>();
            if (path != "/" && path != ".")
                withParent.Add(new SftpEntry { Name = "..", FullPath = Parent(path), IsDirectory = true });
            withParent.AddRange(entries);
            _adapter.Submit(withParent);
        }
        catch (Exception ex)
        {
            Snackbar.Make(_list, ex.Message, Snackbar.LengthLong)!.Show();
        }
        finally { _refresh.Refreshing = false; }
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
        if (entry.Name == "..") return;
        new MaterialAlertDialogBuilder(Activity!)
            .SetTitle(entry.Name)!
            .SetItems(new[] { GetString(Resource.String.delete) }, (_, args) =>
            {
                if (args.Which == 0) _ = DeleteAsync(entry);
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
        catch (Exception ex) { Snackbar.Make(_list, ex.Message, Snackbar.LengthLong)!.Show(); }
    }

    private async void OnUploadPicked(Java.Lang.Object? result)
    {
        if (result is not Android.Net.Uri uri) return;
        var service = ((SessionActivity)Activity!).Service;
        if (service == null) return;
        var name = QueryDisplayName(uri) ?? Guid.NewGuid().ToString("N");
        var target = (_currentPath.EndsWith("/") ? _currentPath : _currentPath + "/") + name;

        Snackbar.Make(_list, GetString(Resource.String.uploading, name)!, Snackbar.LengthLong)!.Show();
        try
        {
            using var stream = Activity!.ContentResolver!.OpenInputStream(uri)!;
            await service.SftpUploadAsync(stream, target, null, CancellationToken.None);
            await LoadAsync(_currentPath);
        }
        catch (Exception ex) { Snackbar.Make(_list, ex.Message, Snackbar.LengthLong)!.Show(); }
    }

    private async void OnDownloadTarget(Java.Lang.Object? result)
    {
        if (result is not Android.Net.Uri uri || _pendingDownload == null) return;
        var service = ((SessionActivity)Activity!).Service;
        if (service == null) return;
        var entry = _pendingDownload;
        _pendingDownload = null;
        try
        {
            using var stream = Activity!.ContentResolver!.OpenOutputStream(uri)!;
            await service.SftpDownloadAsync(entry.FullPath, stream, null, CancellationToken.None);
            Snackbar.Make(_list, GetString(Resource.String.downloaded, entry.Name)!, Snackbar.LengthLong)!.Show();
        }
        catch (Exception ex) { Snackbar.Make(_list, ex.Message, Snackbar.LengthLong)!.Show(); }
    }

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

    private class ResultCallback : Java.Lang.Object, IActivityResultCallback
    {
        private readonly Action<Java.Lang.Object?> _onResult;
        public ResultCallback(Action<Java.Lang.Object?> onResult) => _onResult = onResult;
        public void OnActivityResult(Java.Lang.Object? result) => _onResult(result);
    }
}
