using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AeroSSH.Activities;
using AeroSSH.Models;
using Google.Android.Material.TextField;

namespace AeroSSH.Fragments;

public class CommandFragment : Fragment
{
    private AutoCompleteTextView _input = null!;
    private TextView _output = null!;
    private Button _run = null!;
    private ScrollView _scroll = null!;
    private CancellationTokenSource? _cts;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var view = inflater.Inflate(Resource.Layout.fragment_command, container, false)!;
        _input = view.FindViewById<AutoCompleteTextView>(Resource.Id.commandInput)!;
        _output = view.FindViewById<TextView>(Resource.Id.commandOutput)!;
        _run = view.FindViewById<Button>(Resource.Id.btnRun)!;
        _scroll = view.FindViewById<ScrollView>(Resource.Id.outputScroll)!;
        _run.Click += async (_, _) => await RunAsync();
        return view;
    }

    public override void OnResume()
    {
        base.OnResume();
        var activity = (SessionActivity)Activity!;
        var session = AeroSshApplication.Instance.Sessions.GetSession(activity.SessionId!);
        if (session == null) return;

        var history = AeroSshApplication.Instance.History.Get(session.Profile.Id).ToArray();
        var adapter = new ArrayAdapter<string>(activity, Android.Resource.Layout.SimpleDropDownItem1Line, history);
        _input.Adapter = adapter;
        _input.Threshold = 1;
    }

    private async Task RunAsync()
    {
        var command = _input.Text?.Trim();
        if (string.IsNullOrEmpty(command)) return;

        var activity = (SessionActivity)Activity!;
        var service = activity.Service;
        var session = AeroSshApplication.Instance.Sessions.GetSession(activity.SessionId!);
        if (service == null || session == null) return;

        _run.Enabled = false;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        AppendOutput($"$ {command}\n");
        AeroSshApplication.Instance.History.Add(session.Profile.Id, command);
        AeroSshApplication.Instance.Logs.Add(session.Id, command, LogLevel.Info, LogSource.Command);

        try
        {
            var result = await service.ExecuteCommandAsync(command, _cts.Token);
            if (!string.IsNullOrEmpty(result.Stdout))
            {
                AppendOutput(result.Stdout);
                AeroSshApplication.Instance.Logs.Add(session.Id, result.Stdout, LogLevel.Info, LogSource.Stdout);
            }
            if (!string.IsNullOrEmpty(result.Stderr))
            {
                AppendOutput(result.Stderr);
                AeroSshApplication.Instance.Logs.Add(session.Id, result.Stderr, LogLevel.Warning, LogSource.Stderr);
            }
            AppendOutput($"[exit {result.ExitCode}]\n");
        }
        catch (Exception ex)
        {
            AppendOutput($"error: {ex.Message}\n");
            AeroSshApplication.Instance.Logs.Add(session.Id, ex.Message, LogLevel.Error);
        }
        finally
        {
            _run.Enabled = true;
            _input.Text = string.Empty;
        }
    }

    private void AppendOutput(string text)
    {
        _output.Append(text);
        _scroll.Post(() => _scroll.FullScroll(FocusSearchDirection.Down));
    }

    public override void OnDestroyView()
    {
        _cts?.Cancel();
        base.OnDestroyView();
    }
}
