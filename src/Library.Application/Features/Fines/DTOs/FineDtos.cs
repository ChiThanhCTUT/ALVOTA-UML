using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Fines.DTOs;

public record FinePaymentDto(
    Guid Id,
    Guid FineId,
    decimal Amount,
    DateTime PaidAtUtc,
    string PaymentMethod,
    string ReceiptNumber,
    string ReceivedBy,
    string? Notes)
{
    public static FinePaymentDto From(Payment p) => new(
        p.Id,
        p.FineId,
        p.Amount,
        p.PaidAtUtc,
        p.PaymentMethod,
        p.ReceiptNumber,
        p.ReceivedBy,
        p.Notes);
}

public record FineDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string CardNumber,
    Guid? LoanItemId,
    string? BookTitle,
    decimal Amount,
    FineReason Reason,
    FineStatus Status,
    string? Description,
    string? WaivedReason,
    string? WaivedBy,
    DateTime? WaivedAtUtc,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<FinePaymentDto> Payments)
{
    public static FineDto From(Fine fine) => new(
        fine.Id,
        fine.MemberId,
        fine.Member?.FullName ?? string.Empty,
        fine.Member?.CardNumber ?? string.Empty,
        fine.LoanItemId,
        fine.LoanItem?.BookCopy?.Book?.Title,
        fine.Amount,
        fine.Reason,
        fine.Status,
        fine.Description,
        fine.WaivedReason,
        fine.WaivedBy,
        fine.WaivedAtUtc,
        fine.PaidAmount,
        fine.RemainingAmount,
        fine.Payments.Where(p => !p.IsDeleted).Select(FinePaymentDto.From).ToList());
}
