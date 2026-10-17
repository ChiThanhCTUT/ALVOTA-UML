using Library.Domain.Entities;

namespace Library.Application.Features.System.DTOs;

public record SettingDto(
    string Key,
    string Value,
    string? Description,
    string Category);

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Username,
    string Action,
    string EntityName,
    string? EntityId,
    string? Details,
    DateTime TimestampUtc)
{
    public static AuditLogDto From(AuditLog log) => new(
        log.Id,
        log.UserId,
        log.Username,
        log.Action,
        log.EntityName,
        log.EntityId,
        log.Details,
        log.TimestampUtc);
}
