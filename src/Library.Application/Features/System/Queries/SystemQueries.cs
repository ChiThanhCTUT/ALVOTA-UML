using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.System.DTOs;
using Library.Domain.Common;
using MediatR;

namespace Library.Application.Features.System.Queries;

// SYS-01: Lấy danh sách cấu hình
public sealed record GetSettingsQuery : IRequest<Result<IReadOnlyList<SettingDto>>>;

public sealed class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Result<IReadOnlyList<SettingDto>>>
{
    private readonly ISettingRepository _settings;

    public GetSettingsQueryHandler(ISettingRepository settings)
    {
        _settings = settings;
    }

    public async Task<Result<IReadOnlyList<SettingDto>>> Handle(GetSettingsQuery request, CancellationToken ct)
    {
        var list = await _settings.GetAllAsync(ct);
        var dtos = list.Select(s => new SettingDto(s.Key, s.Value, s.Description, s.Category)).ToList();
        return Result.Success<IReadOnlyList<SettingDto>>(dtos);
    }
}

// SYS-04: Xem và lọc audit log (chỉ Admin)
public sealed record GetAuditLogsQuery(PagedRequest Paging, string? EntityName = null) : IRequest<Result<PagedResult<AuditLogDto>>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUserService _currentUser;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogs, ICurrentUserService currentUser)
    {
        _auditLogs = auditLogs;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        if (_currentUser.Role != "Admin")
        {
            return Result.Failure<PagedResult<AuditLogDto>>(DomainErrors.Auth.Unauthorized);
        }

        var paged = await _auditLogs.GetPagedAsync(request.Paging, request.EntityName, ct);
        var dtos = paged.Items.Select(AuditLogDto.From).ToList();

        return Result.Success(new PagedResult<AuditLogDto>(
            dtos,
            paged.TotalCount,
            paged.PageIndex,
            paged.PageSize));
    }
}
