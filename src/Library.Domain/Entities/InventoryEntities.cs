using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class InventorySession : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ShelfLocation { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public InventorySessionStatus Status { get; set; } = InventorySessionStatus.InProgress;
    public int TotalExpected { get; set; }
    public int TotalScanned { get; set; }
    public int TotalMissing { get; set; }
    public int TotalDamaged { get; set; }
    public string? Notes { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}

public sealed class InventoryItem : AuditableEntity
{
    public Guid InventorySessionId { get; set; }
    public InventorySession? InventorySession { get; set; }
    public Guid BookCopyId { get; set; }
    public BookCopy? BookCopy { get; set; }
    public string ScannedBarcode { get; set; } = string.Empty;
    public DateTime? ScannedAtUtc { get; set; }
    public CopyStatus SystemStatus { get; set; }
    public CopyStatus? ScannedStatus { get; set; }
    public InventoryResultStatus ResultStatus { get; set; } = InventoryResultStatus.Missing;
    public string? Notes { get; set; }
}
