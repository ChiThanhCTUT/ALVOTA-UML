using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Circulation.DTOs;

public record LoanItemDto(
    Guid Id,
    Guid LoanId,
    Guid BookCopyId,
    string BookTitle,
    string Barcode,
    string ShelfLocation,
    DateTime DueDateUtc,
    DateTime? ReturnedAtUtc,
    int RenewalCount,
    ReturnCondition? ReturnCondition,
    bool IsOverdue,
    int OverdueDays)
{
    public static LoanItemDto From(LoanItem item, DateTime utcNow)
    {
        var isOverdue = item.IsOverdue(utcNow);
        var overdueDays = isOverdue ? (int)Math.Ceiling((utcNow - item.DueDateUtc).TotalDays) : 0;

        return new(
            item.Id,
            item.LoanId,
            item.BookCopyId,
            item.BookCopy?.Book?.Title ?? string.Empty,
            item.BookCopy?.Barcode ?? string.Empty,
            item.BookCopy?.ShelfLocation ?? string.Empty,
            item.DueDateUtc,
            item.ReturnedAtUtc,
            item.RenewalCount,
            item.ReturnCondition,
            isOverdue,
            overdueDays);
    }
}

public record LoanDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string CardNumber,
    DateTime BorrowDateUtc,
    DateTime DueDateUtc,
    LoanStatus Status,
    IReadOnlyList<LoanItemDto> Items)
{
    public static LoanDto From(Loan loan, DateTime utcNow) => new(
        loan.Id,
        loan.MemberId,
        loan.Member?.FullName ?? string.Empty,
        loan.Member?.CardNumber ?? string.Empty,
        loan.BorrowDateUtc,
        loan.DueDateUtc,
        loan.Status,
        loan.Items.Where(i => !i.IsDeleted).Select(i => LoanItemDto.From(i, utcNow)).ToList());
}

public record ReturnResultDto(
    Guid LoanItemId,
    string Barcode,
    string BookTitle,
    string MemberName,
    ReturnCondition ReturnCondition,
    int OverdueDays,
    decimal FineAmount,
    Guid? FineId,
    bool HasReservationNotified);

public record ReservationDto(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    string CardNumber,
    DateTime ReservedAtUtc,
    ReservationStatus Status,
    DateTime? NotifiedAtUtc,
    DateTime? HoldExpiresAtUtc)
{
    public static ReservationDto From(Reservation res) => new(
        res.Id,
        res.BookId,
        res.Book?.Title ?? string.Empty,
        res.MemberId,
        res.Member?.FullName ?? string.Empty,
        res.Member?.CardNumber ?? string.Empty,
        res.ReservedAtUtc,
        res.Status,
        res.NotifiedAtUtc,
        res.HoldExpiresAtUtc);
}
