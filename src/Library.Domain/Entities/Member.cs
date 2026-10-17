using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Member : AuditableEntity
{
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public MemberType Type { get; set; } = MemberType.Student;
    public string ClassOrDepartment { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? PhotoPath { get; set; }
    public CardStatus CardStatus { get; set; } = CardStatus.Active;
    public DateTime CardIssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CardExpiresAtUtc { get; set; }
    public int ReissueCount { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public bool IsCardActive(DateTime utcNow)
    {
        return CardStatus == CardStatus.Active && CardExpiresAtUtc > utcNow;
    }

    public int GetBorrowLimit(int? customStudentLimit = null, int? customTeacherLimit = null)
    {
        return Type switch
        {
            MemberType.Student => customStudentLimit ?? BusinessRuleConstants.StudentBorrowLimit,
            MemberType.Teacher => customTeacherLimit ?? BusinessRuleConstants.TeacherBorrowLimit,
            _ => BusinessRuleConstants.StudentBorrowLimit
        };
    }

    public int GetLoanDurationDays(int? customStudentDays = null, int? customTeacherDays = null)
    {
        return Type switch
        {
            MemberType.Student => customStudentDays ?? BusinessRuleConstants.StudentLoanDurationDays,
            MemberType.Teacher => customTeacherDays ?? BusinessRuleConstants.TeacherLoanDurationDays,
            _ => BusinessRuleConstants.StudentLoanDurationDays
        };
    }

    public decimal GetUnpaidFineTotal()
    {
        return Fines
            .Where(f => !f.IsDeleted && f.Status == FineStatus.Pending)
            .Sum(f => f.Amount);
    }

    public void ReissueCard(string newCardNumber, DateTime utcNow, DateTime newExpiryUtc)
    {
        CardNumber = newCardNumber;
        CardIssuedAtUtc = utcNow;
        CardExpiresAtUtc = newExpiryUtc;
        ReissueCount++;
        CardStatus = CardStatus.Active;
    }
}
