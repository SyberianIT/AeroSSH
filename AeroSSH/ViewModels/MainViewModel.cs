using AeroSSH.Models;
using AeroSSH.Services;
using System.Collections.ObjectModel;

namespace AeroSSH.ViewModels
{
    /// <summary>ViewModel для главного экрана (MVVM)</summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SessionManager _sessionManager = new();
        private readonly LogService _logService = new();
        private string _currentSessionId;
        private string _commandInput;
        private bool _isBusy;
        private string _status = "Отключено";

        public ObservableCollection<SshSession> Sessions { get; } = new();
        public ObservableCollection<LogEntry> CurrentLogs { get; } = new();

        public Command ConnectCommand { get; }
        public Command<string> ExecuteCommand { get; }
        public Command DisconnectCommand { get; }
        public Command ExportLogsCommand { get; }

        public string CommandInput
        {
            get => _commandInput;
            set { _commandInput = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            ConnectCommand = new Command(ConnectAsync);
            ExecuteCommand = new Command<string>(ExecuteAsync);
            DisconnectCommand = new Command(DisconnectAsync);
            ExportLogsCommand = new Command(ExportAsync);
        }

        private async void ConnectAsync()
        {
            IsBusy = true;
            var session = new SshSession
            {
                Host = "example.com",
                Port = 22,
                Username = "user",
                Password = "pass"
            };

            _currentSessionId = await _sessionManager.CreateSessionAsync(session);
            if (_currentSessionId != null)
            {
                Sessions.Add(session);
                Status = "✓ Подключено";
                _logService.Log(_currentSessionId, "SSH подключение установлено");
            }
            IsBusy = false;
        }

        private async void ExecuteAsync(string command)
        {
            if (string.IsNullOrEmpty(_currentSessionId)) return;
            IsBusy = true;
            
            try
            {
                var service = _sessionManager.GetService(_currentSessionId);
                var result = await service.ExecuteCommandAsync(command, CancellationToken.None);
                _logService.Log(_currentSessionId, result, "INFO", "STDOUT");
                CurrentLogs.Add(new LogEntry { SessionId = _currentSessionId, Message = result, Source = "STDOUT" });
            }
            catch (Exception ex)
            {
                _logService.Log(_currentSessionId, ex.Message, "ERROR");
            }
            IsBusy = false;
        }

        private async void DisconnectAsync()
        {
            if (_currentSessionId != null)
            {
                await _sessionManager.RemoveSessionAsync(_currentSessionId);
                Status = "Отключено";
                _logService.Log(_currentSessionId, "SSH отключено");
                _currentSessionId = null;
            }
        }

        private async void ExportAsync()
        {
            if (_currentSessionId != null)
                await _logService.ExportLogsAsync(_currentSessionId);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
