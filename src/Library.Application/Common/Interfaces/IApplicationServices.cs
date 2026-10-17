using Library.Application.Common.Models;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}

public interface IBarcodeGenerator
{
    byte[] GenerateCode128Barcode(string content, int width = 300, int height = 90);
}

public interface IExcelService
{
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName);
    (List<T> SuccessRows, List<(int RowNumber, string ErrorMessage)> Errors) ImportFromExcel<T>(
        Stream stream, Func<IReadOnlyDictionary<string, string>, Result<T>> rowParser);
}

public interface IPdfService
{
    byte[] GenerateReceiptPdf(string title, string receiptNumber, string readerName, string cardNumber, decimal amount, string details, DateTime date);
    byte[] GenerateReportPdf<T>(string title, IEnumerable<T> rows, string[] headers, Func<T, string[]> rowMapper);
}

public interface IBackupService
{
    Task<string> CreateBackupAsync(string targetDirectory, CancellationToken ct = default);
    Task RestoreBackupAsync(string backupFilePath, CancellationToken ct = default);
    Task<IReadOnlyList<FileInfo>> GetBackupListAsync(string targetDirectory, CancellationToken ct = default);
}
