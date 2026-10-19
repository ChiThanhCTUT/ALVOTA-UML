using Library.Domain.Entities;

namespace Library.Application.Features.System.DTOs;

public sealed class SettingDto
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }

    public SettingDto(string key, string value, string? description, string category)
    {
        Key = key;
        Value = value;
        Description = description;
        Category = category;
    }
}

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
