using AeroSSH.Models;

namespace AeroSSH.Services;

public interface ISshService : IDisposable
{
    bool IsConnected { get; }
    ServerProfile Profile { get; }

    event EventHandler<string>? ShellDataReceived;

    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync();

    Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken ct);

    Task OpenShellAsync(int columns, int rows, CancellationToken ct);
    Task SendShellAsync(string data, CancellationToken ct);
    Task CloseShellAsync();

    Task<IReadOnlyList<SftpEntry>> SftpListDirectoryAsync(string path, CancellationToken ct);
    Task SftpUploadAsync(Stream localData, string remotePath, IProgress<long>? progress, CancellationToken ct);
    Task SftpDownloadAsync(string remotePath, Stream localData, IProgress<long>? progress, CancellationToken ct);
    Task SftpDeleteAsync(string remotePath, bool isDirectory, CancellationToken ct);
}

public record CommandResult(int ExitCode, string Stdout, string Stderr);
