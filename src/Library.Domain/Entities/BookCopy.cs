using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class BookCopy : AuditableEntity
{
    public Guid BookId { get; set; }
    public Book? Book { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ShelfLocation { get; set; } = string.Empty;
    public CopyStatus Status { get; set; } = CopyStatus.Available;
    public string? ConditionNote { get; set; }

    public ICollection<LoanItem> LoanItems { get; set; } = new List<LoanItem>();
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();

    public bool CanBeBorrowed => Status == CopyStatus.Available;

    public void MarkBorrowed()
    {
        Status = CopyStatus.Borrowed;
    }

    public void MarkReturned(ReturnCondition condition)
    {
        Status = condition switch
        {
            ReturnCondition.Normal => CopyStatus.Available,
            ReturnCondition.DamagedLight => CopyStatus.Damaged,
            ReturnCondition.DamagedHeavy => CopyStatus.Damaged,
            ReturnCondition.Lost => CopyStatus.Lost,
            _ => CopyStatus.Available
        };
    }

    public void MarkReserved()
    {
        Status = CopyStatus.Reserved;
    }

    public void MarkAvailable()
    {
        Status = CopyStatus.Available;
    }

    public void MarkDisposed()
    {
        Status = CopyStatus.Disposed;
    }
}
