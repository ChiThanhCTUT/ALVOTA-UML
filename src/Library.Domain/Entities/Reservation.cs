using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Reservation : AuditableEntity
{
    public Guid BookId { get; set; }
    public Book? Book { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public Guid? ReservedCopyId { get; set; }
    public BookCopy? ReservedCopy { get; set; }
    public DateTime ReservedAtUtc { get; set; } = DateTime.UtcNow;
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime? NotifiedAtUtc { get; set; }
    public DateTime? HoldExpiresAtUtc { get; set; }

    public bool IsExpired(DateTime utcNow)
    {
        return Status == ReservationStatus.Notified && HoldExpiresAtUtc.HasValue && utcNow > HoldExpiresAtUtc.Value;
    }

    /// <summary>
    /// BR-10: Giữ chỗ đặt trước 3 ngày sau khi có sách trả
    /// </summary>
    public void NotifyAvailable(BookCopy copy, DateTime utcNow, int holdDays = BusinessRuleConstants.ReservationHoldDays)
    {
        ReservedCopyId = copy.Id;
        Status = ReservationStatus.Notified;
        NotifiedAtUtc = utcNow;
        HoldExpiresAtUtc = utcNow.AddDays(holdDays);
        UpdatedAtUtc = utcNow;
    }

    public void Fulfill(DateTime utcNow)
    {
        Status = ReservationStatus.Completed;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        Status = ReservationStatus.Cancelled;
        UpdatedAtUtc = utcNow;
    }

    public void Expire(DateTime utcNow)
    {
        Status = ReservationStatus.Expired;
        UpdatedAtUtc = utcNow;
    }
}
