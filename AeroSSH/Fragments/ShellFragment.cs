using Android.OS;
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

        var fontSize = AeroSshApplication.Instance.AppPrefs.TerminalFontSize;
        _output.SetTextSize(Android.Util.ComplexUnitType.Sp, fontSize);
        _input.SetTextSize(Android.Util.ComplexUnitType.Sp, fontSize);

        _send.Click += async (_, _) => await SendInputAsync();
        _input.EditorAction += async (_, e) =>
        {
            if (e.ActionId == ImeAction.Send || e.ActionId == ImeAction.Done)
            {
                await SendInputAsync();
                e.Handled = true;
            }
        };

        WireQuickKey(view, Resource.Id.keyTab, "\t");
        WireQuickKey(view, Resource.Id.keyEsc, "\x1B");
        WireQuickKey(view, Resource.Id.keyCtrlC, "\x03", clearInput: true);
        WireQuickKey(view, Resource.Id.keyCtrlD, "\x04");
        WireQuickKey(view, Resource.Id.keyCtrlL, "\x0C");
        WireQuickKey(view, Resource.Id.keyCtrlZ, "\x1A");
        WireQuickKey(view, Resource.Id.keyUp, "\x1B[A");
        WireQuickKey(view, Resource.Id.keyDown, "\x1B[B");
        WireQuickKey(view, Resource.Id.keyLeft, "\x1B[D");
        WireQuickKey(view, Resource.Id.keyRight, "\x1B[C");
        WireQuickKey(view, Resource.Id.keyPipe, "|");
        WireQuickKey(view, Resource.Id.keyTilde, "~");
        WireQuickKey(view, Resource.Id.keySlash, "/");

        return view;
    }

    private void WireQuickKey(View root, int id, string sequence, bool clearInput = false)
    {
        var btn = root.FindViewById<Button>(id);
        if (btn == null) return;
        btn.Click += async (_, _) =>
        {
            if (_service == null) return;
            try { await _service.SendShellAsync(sequence, CancellationToken.None); }
            catch (Exception ex) { Append($"\nsend error: {ex.Message}\n"); }
            if (clearInput) _input.Text = string.Empty;
        };
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
        var sanitized = AnsiUtils.Sanitize(text);
        if (sanitized.Length == 0) return;
        Activity?.RunOnUiThread(() => Append(sanitized));
    }

    private void Append(string text)
    {
        _output.Append(text);
        _scroll.Post(() => _scroll.FullScroll(FocusSearchDirection.Down));
    }
}
