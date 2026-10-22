using Library.Application.Common.Interfaces;
using Library.Domain.Common;
using Library.Domain.Entities;
using MediatR;

namespace Library.Application.Features.System.Commands;

// SYS-01: Cấu hình hệ thống
public sealed record UpdateSettingCommand(
    string Key,
    string Value,
    string Category,
    string? Description = null) : IRequest<Result>;

public sealed class UpdateSettingCommandHandler : IRequestHandler<UpdateSettingCommand, Result>
{
    private readonly ISettingRepository _settings;
    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public UpdateSettingCommandHandler(
        ISettingRepository settings,
        IAuditLogRepository auditLogs,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _settings = settings;
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _uow = uow;
        _time = time;
    }

    public async Task<Result> Handle(UpdateSettingCommand request, CancellationToken ct)
    {
        if (_currentUser.Role != "Admin")
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var now = _time.GetUtcNow().UtcDateTime;
        await _settings.SetValueAsync(request.Key, request.Value, request.Category, request.Description, ct);

        var audit = new AuditLog
        {
            Username = _currentUser.Username ?? "Admin",
            Action = "UpdateSetting",
            EntityName = nameof(Setting),
            EntityId = request.Key,
            Details = $"Cập nhật thiết lập {request.Key} = {request.Value}",
            TimestampUtc = now
        };

        await _auditLogs.AddAsync(audit, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// SYS-02: Sao lưu thủ công
public sealed record CreateBackupCommand(string TargetDirectory) : IRequest<Result<string>>;

public sealed class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, Result<string>>
{
    private readonly IBackupService _backupService;
    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CreateBackupCommandHandler(
        IBackupService backupService,
        IAuditLogRepository auditLogs,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _backupService = backupService;
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<string>> Handle(CreateBackupCommand request, CancellationToken ct)
    {
        if (_currentUser.Role != "Admin" && _currentUser.Role != "Librarian")
            return Result.Failure<string>(DomainErrors.Auth.Unauthorized);

        var now = _time.GetUtcNow().UtcDateTime;
        try
        {
            var filePath = await _backupService.CreateBackupAsync(request.TargetDirectory, ct);

            var audit = new AuditLog
            {
                Username = _currentUser.Username ?? "Admin",
                Action = "CreateBackup",
                EntityName = "Database",
                EntityId = filePath,
                Details = $"Sao lưu dữ liệu ra file {Path.GetFileName(filePath)}",
                TimestampUtc = now
            };
            await _auditLogs.AddAsync(audit, ct);
            await _uow.SaveChangesAsync(ct);

            return Result.Success(filePath);
        }
        catch (Exception)
        {
            return Result.Failure<string>(DomainErrors.System.BackupFailed);
        }
    }
}

// SYS-02: Phục hồi từ file sao lưu
public sealed record RestoreBackupCommand(string BackupFilePath) : IRequest<Result>;

public sealed class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, Result>
{
    private readonly IBackupService _backupService;
    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public RestoreBackupCommandHandler(
        IBackupService backupService,
        IAuditLogRepository auditLogs,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _backupService = backupService;
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _uow = uow;
        _time = time;
    }

    public async Task<Result> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        if (_currentUser.Role != "Admin")
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var now = _time.GetUtcNow().UtcDateTime;
        try
        {
            await _backupService.RestoreBackupAsync(request.BackupFilePath, ct);

            var audit = new AuditLog
            {
                Username = _currentUser.Username ?? "Admin",
                Action = "RestoreBackup",
                EntityName = "Database",
                EntityId = request.BackupFilePath,
                Details = $"Phục hồi dữ liệu từ file {Path.GetFileName(request.BackupFilePath)}",
                TimestampUtc = now
            };
            await _auditLogs.AddAsync(audit, ct);
            await _uow.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(DomainErrors.System.RestoreFailed);
        }
    }
}
