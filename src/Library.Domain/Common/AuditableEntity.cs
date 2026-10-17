namespace Library.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }

    public void MarkDeleted(string user, DateTime utcNow)
    {
        IsDeleted = true;
        UpdatedAtUtc = utcNow;
        UpdatedBy = user;
    }
}
