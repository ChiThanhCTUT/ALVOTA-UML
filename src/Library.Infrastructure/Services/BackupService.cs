using Library.Application.Common.Interfaces;

namespace Library.Infrastructure.Services;

// SYS-02, NFR-09: Tự sao lưu và phục hồi, giữ 14 bản gần nhất
public sealed class BackupService : IBackupService
{
    private const int MaxBackupRetentionCount = 14;
    private readonly string _databaseFilePath;

    public BackupService(string databaseFilePath = "library.db")
    {
        _databaseFilePath = databaseFilePath;
    }

    public async Task<string> CreateBackupAsync(string targetDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var fileName = $"library_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db";
        var destPath = Path.Combine(targetDirectory, fileName);

        if (File.Exists(_databaseFilePath))
        {
            await using var sourceStream = new FileStream(_databaseFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destStream, ct);
        }
        else
        {
            // If DB doesn't exist on disk yet, touch the file
            await File.WriteAllBytesAsync(destPath, Array.Empty<byte>(), ct);
        }

        PruneOldBackups(targetDirectory);
        return destPath;
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("File sao lưu không tồn tại.", backupFilePath);

        await Task.Run(() => File.Copy(backupFilePath, _databaseFilePath, overwrite: true), ct);
    }

    public async Task<IReadOnlyList<FileInfo>> GetBackupListAsync(string targetDirectory, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!Directory.Exists(targetDirectory))
                return Array.Empty<FileInfo>();

            var dir = new DirectoryInfo(targetDirectory);
            return (IReadOnlyList<FileInfo>)dir.GetFiles("library_backup_*.db")
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();
        }, ct);
    }

    private static void PruneOldBackups(string targetDirectory)
    {
        try
        {
            var dir = new DirectoryInfo(targetDirectory);
            var files = dir.GetFiles("library_backup_*.db")
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (files.Count > MaxBackupRetentionCount)
            {
                var filesToDelete = files.Skip(MaxBackupRetentionCount);
                foreach (var file in filesToDelete)
                {
                    try { file.Delete(); } catch { /* ignore individual delete failures */ }
                }
            }
        }
        catch
        {
            // Logging can happen at outer level
        }
    }
}
