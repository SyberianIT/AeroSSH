using System.Text;
using AeroSSH.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace AeroSSH.Services;

public class SshServiceImpl : ISshService
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(60);

    private readonly HostKeyStore _hostKeys;
    private readonly object _sync = new();

    private SshClient? _ssh;
    private SftpClient? _sftp;
    private ShellStream? _shell;
    private CancellationTokenSource? _shellPump;
    private bool _disposed;

    public ServerProfile Profile { get; }
    public bool IsConnected => _ssh?.IsConnected == true;

    public event EventHandler<string>? ShellDataReceived;

    public SshServiceImpl(ServerProfile profile, HostKeyStore hostKeys)
    {
        Profile = profile;
        _hostKeys = hostKeys;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        var connInfo = BuildConnectionInfo(Profile);
        var ssh = new SshClient(connInfo) { KeepAliveInterval = KeepAlive };
        ssh.HostKeyReceived += OnHostKeyReceived;

        try
        {
            await Task.Run(() => ssh.Connect(), ct).ConfigureAwait(false);
        }
        catch
        {
            ssh.Dispose();
            throw;
        }

        lock (_sync) _ssh = ssh;
    }

    private void OnHostKeyReceived(object? sender, HostKeyEventArgs e)
    {
        var fingerprint = HostKeyStore.ComputeFingerprint(e.HostKey);
        var known = _hostKeys.GetFingerprint(Profile.Host, Profile.Port);

        if (known == null)
        {
            e.CanTrust = false;
            throw new HostKeyVerificationException(Profile.Host, Profile.Port, fingerprint, null);
        }

        if (!string.Equals(known, fingerprint, StringComparison.Ordinal))
        {
            e.CanTrust = false;
            throw new HostKeyVerificationException(Profile.Host, Profile.Port, fingerprint, known);
        }

        e.CanTrust = true;
    }

    public Task DisconnectAsync()
    {
        return Task.Run(() =>
        {
            SshClient? ssh;
            lock (_sync) { ssh = _ssh; _ssh = null; }
            try { if (ssh?.IsConnected == true) ssh.Disconnect(); } catch { /* ignore */ }
        });
    }

    public async Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken ct)
    {
        EnsureConnected();
        return await Task.Run(() =>
        {
            using var cmd = _ssh!.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromMinutes(5);
            var stdout = cmd.Execute();
            var stderr = cmd.Error ?? string.Empty;
            return new CommandResult(cmd.ExitStatus ?? -1, stdout ?? string.Empty, stderr);
        }, ct).ConfigureAwait(false);
    }

    public async Task OpenShellAsync(int columns, int rows, CancellationToken ct)
    {
        EnsureConnected();
        await CloseShellAsync().ConfigureAwait(false);

        var stream = _ssh!.CreateShellStream("xterm-256color", (uint)columns, (uint)rows, 800, 600, 8192);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        lock (_sync) { _shell = stream; _shellPump = cts; }

        _ = Task.Run(() => PumpShellAsync(stream, cts.Token), cts.Token);
    }

    private async Task PumpShellAsync(ShellStream stream, CancellationToken ct)
    {
        var buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!stream.CanRead) break;
                if (stream.DataAvailable)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                    if (read <= 0) break;
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    ShellDataReceived?.Invoke(this, text);
                }
                else
                {
                    await Task.Delay(50, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    public Task SendShellAsync(string data, CancellationToken ct)
    {
        ShellStream? stream;
        lock (_sync) stream = _shell;
        if (stream == null) throw new InvalidOperationException("Shell is not open");
        return Task.Run(() =>
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }, ct);
    }

    public Task CloseShellAsync()
    {
        ShellStream? stream;
        CancellationTokenSource? cts;
        lock (_sync) { stream = _shell; cts = _shellPump; _shell = null; _shellPump = null; }
        try { cts?.Cancel(); } catch { }
        try { stream?.Close(); } catch { }
        try { stream?.Dispose(); } catch { }
        cts?.Dispose();
        return Task.CompletedTask;
    }

    private SftpClient EnsureSftp()
    {
        EnsureConnected();
        lock (_sync)
        {
            if (_sftp == null || !_sftp.IsConnected)
            {
                _sftp?.Dispose();
                _sftp = new SftpClient(_ssh!.ConnectionInfo);
                _sftp.Connect();
            }
            return _sftp;
        }
    }

    public Task<IReadOnlyList<SftpEntry>> SftpListDirectoryAsync(string path, CancellationToken ct)
    {
        return Task.Run<IReadOnlyList<SftpEntry>>(() =>
        {
            var client = EnsureSftp();
            var list = new List<SftpEntry>();
            foreach (var f in client.ListDirectory(path))
            {
                if (f.Name is "." or "..") continue;
                list.Add(new SftpEntry
                {
                    Name = f.Name,
                    FullPath = f.FullName,
                    IsDirectory = f.IsDirectory,
                    IsSymlink = f.IsSymbolicLink,
                    Length = f.Length,
                    LastWriteTime = f.LastWriteTime
                });
            }
            return list
                .OrderByDescending(x => x.IsDirectory)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }, ct);
    }

    public Task SftpUploadAsync(Stream localData, string remotePath, IProgress<long>? progress, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var client = EnsureSftp();
            client.UploadFile(localData, remotePath, true, bytes =>
            {
                progress?.Report((long)bytes);
                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
            });
        }, ct);
    }

    public Task SftpDownloadAsync(string remotePath, Stream localData, IProgress<long>? progress, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var client = EnsureSftp();
            client.DownloadFile(remotePath, localData, bytes =>
            {
                progress?.Report((long)bytes);
                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
            });
        }, ct);
    }

    public Task SftpDeleteAsync(string remotePath, bool isDirectory, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var client = EnsureSftp();
            if (isDirectory) client.DeleteDirectory(remotePath);
            else client.DeleteFile(remotePath);
        }, ct);
    }

    private ConnectionInfo BuildConnectionInfo(ServerProfile p)
    {
        AuthenticationMethod auth = p.AuthMethod == AuthMethod.PrivateKey && p.PrivateKey is { Length: > 0 }
            ? BuildKeyAuth(p)
            : new PasswordAuthenticationMethod(p.Username, p.Password ?? string.Empty);

        return new ConnectionInfo(p.Host, p.Port, p.Username, auth)
        {
            Timeout = ConnectTimeout
        };
    }

    private static PrivateKeyAuthenticationMethod BuildKeyAuth(ServerProfile p)
    {
        using var ms = new MemoryStream(p.PrivateKey!);
        var keyFile = string.IsNullOrEmpty(p.KeyPassphrase)
            ? new PrivateKeyFile(ms)
            : new PrivateKeyFile(ms, p.KeyPassphrase);
        return new PrivateKeyAuthenticationMethod(p.Username, keyFile);
    }

    private void EnsureConnected()
    {
        if (!IsConnected) throw new InvalidOperationException("SSH connection is not established");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseShellAsync().GetAwaiter().GetResult();
        try { _sftp?.Disconnect(); } catch { }
        _sftp?.Dispose();
        try { _ssh?.Disconnect(); } catch { }
        _ssh?.Dispose();
    }
}
