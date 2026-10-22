using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Fine : AuditableEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public Guid? LoanItemId { get; set; }
    public LoanItem? LoanItem { get; set; }
    public decimal Amount { get; set; }
    public FineReason Reason { get; set; }
    public FineStatus Status { get; set; } = FineStatus.Pending;
    public string? Description { get; set; }
    public string? WaivedReason { get; set; }
    public string? WaivedBy { get; set; }
    public DateTime? WaivedAtUtc { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public decimal PaidAmount => Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount);
    public decimal RemainingAmount => Amount - PaidAmount;

    public void Waive(string waivedBy, string reason, DateTime utcNow)
    {
        Status = FineStatus.Waived;
        WaivedBy = waivedBy;
        WaivedReason = reason;
        WaivedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Payment AddPayment(decimal amount, string receivedBy, string receiptNumber, DateTime utcNow, string? notes = null)
    {
        var payment = new Payment
        {
            FineId = Id,
            Amount = amount,
            ReceivedBy = receivedBy,
            ReceiptNumber = receiptNumber,
            PaidAtUtc = utcNow,
            Notes = notes,
            CreatedAtUtc = utcNow,
            CreatedBy = receivedBy
        };

        Payments.Add(payment);

        if (PaidAmount >= Amount)
        {
            Status = FineStatus.Paid;
        }

        UpdatedAtUtc = utcNow;
        return payment;
    }
}

public sealed class Payment : AuditableEntity
{
    public Guid FineId { get; set; }
    public Fine? Fine { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Cash";
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
