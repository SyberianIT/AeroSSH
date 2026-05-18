using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Fragment.App;
using AeroSSH.Activities;
using AeroSSH.Services;

namespace AeroSSH.Fragments;

public class ShellFragment : Fragment
{
    private TextView _output = null!;
    private EditText _input = null!;
    private Button _send = null!;
    private ScrollView _scroll = null!;
    private CancellationTokenSource? _cts;
    private ISshService? _service;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var view = inflater.Inflate(Resource.Layout.fragment_shell, container, false)!;
        _output = view.FindViewById<TextView>(Resource.Id.shellOutput)!;
        _input = view.FindViewById<EditText>(Resource.Id.shellInput)!;
        _send = view.FindViewById<Button>(Resource.Id.btnSend)!;
        _scroll = view.FindViewById<ScrollView>(Resource.Id.shellScroll)!;
        _send.Click += async (_, _) => await SendInputAsync();
        _input.EditorAction += async (_, e) =>
        {
            if (e.ActionId == ImeAction.Send || e.ActionId == ImeAction.Done)
                await SendInputAsync();
        };
        return view;
    }

    public override async void OnResume()
    {
        base.OnResume();
        var activity = (SessionActivity)Activity!;
        _service = activity.Service;
        if (_service == null) return;

        _service.ShellDataReceived += OnDataReceived;
        _cts = new CancellationTokenSource();
        try { await _service.OpenShellAsync(120, 30, _cts.Token); }
        catch (Exception ex) { Append($"\nshell error: {ex.Message}\n"); }
    }

    public override async void OnPause()
    {
        base.OnPause();
        if (_service != null) _service.ShellDataReceived -= OnDataReceived;
        _cts?.Cancel();
        if (_service != null) await _service.CloseShellAsync();
    }

    private async Task SendInputAsync()
    {
        if (_service == null) return;
        var text = _input.Text ?? string.Empty;
        try { await _service.SendShellAsync(text + "\n", CancellationToken.None); }
        catch (Exception ex) { Append($"\nsend error: {ex.Message}\n"); }
        _input.Text = string.Empty;
    }

    private void OnDataReceived(object? sender, string text)
    {
        Activity?.RunOnUiThread(() => Append(text));
    }

    private void Append(string text)
    {
        _output.Append(text);
        _scroll.Post(() => _scroll.FullScroll(FocusSearchDirection.Down));
    }
}
