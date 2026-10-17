using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Loan : AuditableEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public DateTime BorrowDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public string? Notes { get; set; }

    public ICollection<LoanItem> Items { get; set; } = new List<LoanItem>();

    public bool HasOverdueItems(DateTime utcNow)
    {
        return Items.Any(i => !i.IsDeleted && i.ReturnedAtUtc == null && i.DueDateUtc < utcNow);
    }
}

public sealed class LoanItem : AuditableEntity
{
    public Guid LoanId { get; set; }
    public Loan? Loan { get; set; }
    public Guid BookCopyId { get; set; }
    public BookCopy? BookCopy { get; set; }
    public DateTime DueDateUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public int RenewalCount { get; set; }
    public ReturnCondition? ReturnCondition { get; set; }
    public Guid? FineId { get; set; }
    public Fine? Fine { get; set; }

    public bool IsReturned => ReturnedAtUtc.HasValue;
    public bool IsOverdue(DateTime utcNow) => !IsReturned && utcNow > DueDateUtc;

    /// <summary>
    /// CIR-05, BR-04: Gia hạn tối đa 2 lần, mỗi lần 7 ngày; không gia hạn nếu đã quá hạn hoặc có người đặt trước
    /// </summary>
    public bool CanRenew(DateTime utcNow, bool hasActiveReservation)
    {
        if (IsReturned) return false;
        if (RenewalCount >= BusinessRuleConstants.MaxRenewalCount) return false;
        if (utcNow > DueDateUtc) return false;
        if (hasActiveReservation) return false;
        return true;
    }

    public void Renew(DateTime utcNow, int renewalDays = BusinessRuleConstants.RenewalDays)
    {
        RenewalCount++;
        DueDateUtc = DueDateUtc.AddDays(renewalDays);
        UpdatedAtUtc = utcNow;
    }

    public void MarkReturned(DateTime utcNow, ReturnCondition condition)
    {
        ReturnedAtUtc = utcNow;
        ReturnCondition = condition;
        UpdatedAtUtc = utcNow;
    }
}
