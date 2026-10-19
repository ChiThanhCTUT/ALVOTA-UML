using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Reports.DTOs;

public record DashboardStatisticsDto(
    int TotalBooks,
    int TotalCopies,
    int AvailableCopies,
    int BorrowedCopies,
    int OverdueCount,
    decimal MonthlyFineRevenue,
    int TotalActiveMembers);

public record TopBookDto(
    Guid BookId,
    string Title,
    string CategoryName,
    int BorrowCount);

public record TopReaderDto(
    Guid MemberId,
    string FullName,
    string CardNumber,
    string ClassOrDepartment,
    int BorrowCount);

public record OverdueLoanReportDto(
    Guid LoanItemId,
    string MemberName,
    string CardNumber,
    string ClassOrDepartment,
    string? PhoneNumber,
    string BookTitle,
    string Barcode,
    DateTime BorrowDateUtc,
    DateTime DueDateUtc,
    int OverdueDays,
    decimal EstimatedFine)
{
    public string CopyBarcode => Barcode;
}
