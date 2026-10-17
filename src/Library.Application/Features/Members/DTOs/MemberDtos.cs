using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Members.DTOs;

public record MemberDto(
    Guid Id,
    string CardNumber,
    string FullName,
    DateTime DateOfBirth,
    MemberType Type,
    string ClassOrDepartment,
    string? PhoneNumber,
    string? Email,
    string? PhotoPath,
    CardStatus CardStatus,
    DateTime CardIssuedAtUtc,
    DateTime CardExpiresAtUtc,
    int ReissueCount,
    int ActiveLoansCount,
    decimal UnpaidFineAmount)
{
    public static MemberDto From(Member member, int activeLoans = 0, decimal unpaidFines = 0) => new(
        member.Id,
        member.CardNumber,
        member.FullName,
        member.DateOfBirth,
        member.Type,
        member.ClassOrDepartment,
        member.PhoneNumber,
        member.Email,
        member.PhotoPath,
        member.CardStatus,
        member.CardIssuedAtUtc,
        member.CardExpiresAtUtc,
        member.ReissueCount,
        activeLoans,
        unpaidFines);
}
