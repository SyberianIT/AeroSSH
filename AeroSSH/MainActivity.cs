using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using AeroSSH.Services;
using AeroSSH.Models;
using System.Threading.Tasks;

namespace AeroSSH
{
    [Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@style/AppTheme")]
    public class MainActivity : AppCompatActivity
    {
        private SessionManager _sessionManager;
        private LogService _logService;
        private EditText _hostEdit, _userEdit, _passEdit, _commandEdit;
        private Button _connectBtn, _executeBtn, _disconnectBtn;
        private TextView _statusView, _logView;
        private ScrollView _scrollView;
        private string _currentSessionId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            InitializeServices();
            InitializeViews();
            BindEvents();
        }

        private void InitializeServices()
        {
            _sessionManager = new SessionManager();
            _logService = new LogService();
        }

        private void InitializeViews()
        {
            _hostEdit = FindViewById<EditText>(Resource.Id.hostEdit);
            _userEdit = FindViewById<EditText>(Resource.Id.userEdit);
            _passEdit = FindViewById<EditText>(Resource.Id.passEdit);
            _commandEdit = FindViewById<EditText>(Resource.Id.commandEdit);
            _connectBtn = FindViewById<Button>(Resource.Id.connectBtn);
            _executeBtn = FindViewById<Button>(Resource.Id.runBtn);
            _disconnectBtn = FindViewById<Button>(Resource.Id.disconnectBtn);
            _statusView = FindViewById<TextView>(Resource.Id.statusView);
            _logView = FindViewById<TextView>(Resource.Id.logView);
            _scrollView = FindViewById<ScrollView>(Resource.Id.scrollView);
        }

        private void BindEvents()
        {
            _connectBtn.Click += async (s, e) => await ConnectAsync();
            _executeBtn.Click += async (s, e) => await ExecuteCommandAsync();
            _disconnectBtn.Click += async (s, e) => await DisconnectAsync();
        }

        private async Task ConnectAsync()
        {
            _connectBtn.Enabled = false;
            _statusView.Text = "Подключение...";

            try
            {
                var session = new SshSession
                {
                    Host = _hostEdit.Text,
                    Port = 22,
                    Username = _userEdit.Text,
                    Password = _passEdit.Text
                };

                _currentSessionId = await _sessionManager.CreateSessionAsync(session);
                if (_currentSessionId != null)
                {
                    _statusView.Text = "✓ Подключено";
                    _executeBtn.Enabled = true;
                    _disconnectBtn.Enabled = true;
                    Log($"SSH подключено к {session.Host}");
                }
            }
            catch (Exception ex)
            {
                _statusView.Text = "✗ Ошибка подключения";
                Log($"Ошибка: {ex.Message}", "ERROR");
            }
            finally
            {
                _connectBtn.Enabled = true;
            }
        }

        private async Task ExecuteCommandAsync()
        {
            if (string.IsNullOrEmpty(_currentSessionId)) return;

            var command = _commandEdit.Text;
            if (string.IsNullOrEmpty(command)) return;

            _executeBtn.Enabled = false;
            Log($"> {command}");

            try
            {
                var service = _sessionManager.GetService(_currentSessionId);
                var result = await service.ExecuteCommandAsync(command, CancellationToken.None);
                Log(result);
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}", "ERROR");
            }
            finally
            {
                _executeBtn.Enabled = true;
                _commandEdit.Text = "";
            }
        }

        private async Task DisconnectAsync()
        {
            if (_currentSessionId == null) return;

            await _sessionManager.RemoveSessionAsync(_currentSessionId);
            _statusView.Text = "Отключено";
            _executeBtn.Enabled = false;
            _disconnectBtn.Enabled = false;
            _connectBtn.Enabled = true;
            Log("SSH отключено");
            _currentSessionId = null;
        }

        private void Log(string message, string level = "INFO")
        {
            if (_currentSessionId != null)
            {
                _logService.Log(_currentSessionId, message, level);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _logView.Append($"[{level}] {message}\n");
                _scrollView.Post(() => _scrollView.FullScroll(Android.Views.FocusSearchDirection.Down));
            });
        }
    }
}
