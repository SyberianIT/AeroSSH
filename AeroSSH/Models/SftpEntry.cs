namespace AeroSSH.Models;

public class SftpEntry
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public bool IsSymlink { get; init; }
    public long Length { get; init; }
    public DateTime LastWriteTime { get; init; }
}
