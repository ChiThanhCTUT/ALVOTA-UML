using Library.Domain.Common;

namespace Library.Domain.Entities;

public sealed class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}

public sealed class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public bool IsLockedOut(DateTime utcNow)
    {
        return LockoutEndUtc.HasValue && LockoutEndUtc.Value > utcNow;
    }

    public void RecordFailedLogin(DateTime utcNow, int maxAttempts = BusinessRuleConstants.MaxFailedLoginAttempts, int lockoutMinutes = BusinessRuleConstants.AccountLockoutMinutes)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndUtc = utcNow.AddMinutes(lockoutMinutes);
        }
        UpdatedAtUtc = utcNow;
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Unlock(DateTime utcNow)
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        UpdatedAtUtc = utcNow;
    }
}

public sealed class AuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string Username { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}

public sealed class Setting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
}
