using AeroSSH.Models;

namespace AeroSSH.Services
{
    /// <summary>Интерфейс SSH сервиса</summary>
    public interface ISshService : IDisposable
    {
        Task<bool> ConnectAsync(SshSession session, IProgress<string> progress, CancellationToken ct);
        Task<string> ExecuteCommandAsync(string command, CancellationToken ct);
        Task DisconnectAsync();
        Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken ct);
        Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct);
        IAsyncEnumerable<string> GetShellStreamAsync(CancellationToken ct);
        bool IsConnected { get; }
    }
}
