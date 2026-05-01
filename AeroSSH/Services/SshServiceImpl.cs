using Renci.SshNet;
using Renci.SshNet.Common;
using AeroSSH.Models;
using System.Text;

namespace AeroSSH.Services
{
    /// <summary>Реализация SSH сервиса</summary>
    public class SshServiceImpl : ISshService
    {
        private SshClient _client;
        private SftpClient _sftp;
        private ShellStream _shell;
        private bool _disposed;
        private readonly int _timeout = 30000;

        public bool IsConnected => _client?.IsConnected ?? false;

        public async Task<bool> ConnectAsync(SshSession session, IProgress<string> progress, CancellationToken ct)
        {
            try
            {
                _client?.Dispose();
                var connInfo = session.PrivateKey != null
                    ? CreateKeyAuth(session)
                    : new PasswordConnectionInfo(session.Host, session.Port, session.Username, session.Password)
                    {
                        Timeout = TimeSpan.FromMilliseconds(_timeout)
                    };

                _client = new SshClient(connInfo) { KeepAliveInterval = TimeSpan.FromSeconds(60) };
                await Task.Run(() => _client.Connect(), ct);
                progress?.Report($"✓ Подключено к {session.Username}@{session.Host}:{session.Port}");
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"✗ Ошибка: {ex.Message}");
                return false;
            }
        }

        public async Task<string> ExecuteCommandAsync(string command, CancellationToken ct)
        {
            if (!IsConnected) throw new InvalidOperationException("SSH не подключен");

            var cmd = _client.CreateCommand(command);
            var result = new StringBuilder();

            using var stdout = new StreamReader(cmd.OutputStream, Encoding.UTF8);
            using var stderr = new StreamReader(cmd.ExtendedOutputStream, Encoding.UTF8);

            var asyncResult = cmd.BeginExecute();
            var buffer = new char[4096];

            while (!asyncResult.IsCompleted)
            {
                ct.ThrowIfCancellationRequested();
                while (stdout.Peek() > -1)
                    result.Append(await stdout.ReadAsync(buffer, 0, buffer.Length));
                await Task.Delay(50, ct);
            }

            cmd.EndExecute(asyncResult);
            result.Append(await stdout.ReadToEndAsync());
            result.Append(await stderr.ReadToEndAsync());
            return result.ToString();
        }

        public async Task DisconnectAsync()
        {
            if (_client?.IsConnected == true)
                await Task.Run(() => _client.Disconnect());
        }

        public async Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken ct)
        {
            try
            {
                _sftp ??= new SftpClient(_client.ConnectionInfo);
                if (!_sftp.IsConnected) _sftp.Connect();

                using var file = File.OpenRead(localPath);
                await Task.Run(() => _sftp.UploadFile(file, remotePath), ct);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct)
        {
            try
            {
                _sftp ??= new SftpClient(_client.ConnectionInfo);
                if (!_sftp.IsConnected) _sftp.Connect();

                using var file = File.Create(localPath);
                await Task.Run(() => _sftp.DownloadFile(remotePath, file), ct);
                return true;
            }
            catch { return false; }
        }

        public async IAsyncEnumerable<string> GetShellStreamAsync(System.Collections.Generic.CancellationToken ct)
        {
            _shell ??= _client.CreateShellStream("xterm", 120, 40, 800, 600, 4096);
            var buffer = new byte[4096];

            while (!ct.IsCancellationRequested && IsConnected)
            {
                if (_shell.DataAvailable)
                {
                    yield return _shell.Read();
                }
                await Task.Delay(100, ct);
            }
        }

        private PrivateKeyConnectionInfo CreateKeyAuth(SshSession session)
        {
            var key = new MemoryStream(session.PrivateKey);
            var keyFile = new PrivateKeyFile(key, session.KeyPassphrase);
            return new PrivateKeyConnectionInfo(session.Host, session.Port, session.Username, keyFile)
            {
                Timeout = TimeSpan.FromMilliseconds(_timeout)
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _shell?.Dispose();
            _sftp?.Dispose();
            _client?.Dispose();
            _disposed = true;
        }
    }
}
