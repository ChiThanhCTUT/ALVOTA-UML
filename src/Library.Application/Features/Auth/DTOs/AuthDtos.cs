using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Auth.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string? Email,
    string Role,
    bool IsActive,
    DateTime? LockoutEndUtc,
    DateTime? LastLoginAtUtc = null)
{
    public static UserDto From(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.Role?.Name ?? "Assistant",
        user.IsActive,
        user.LockoutEndUtc,
        user.LastLoginAtUtc);
}

public record LoginResultDto(
    UserDto User,
    string SessionToken);
