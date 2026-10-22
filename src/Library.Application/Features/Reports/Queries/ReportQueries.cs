using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Reports.DTOs;
using Library.Domain.Common;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Reports.Queries;

// RPT-01: Dashboard: tổng đầu sách, tổng bản sao, đang mượn, quá hạn, thu phạt trong tháng
public sealed record GetDashboardStatisticsQuery : IRequest<Result<DashboardStatisticsDto>>;

public sealed class GetDashboardStatisticsQueryHandler : IRequestHandler<GetDashboardStatisticsQuery, Result<DashboardStatisticsDto>>
{
    private readonly IBookRepository _books;
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;
    private readonly IMemberRepository _members;
    private readonly TimeProvider _time;

    public GetDashboardStatisticsQueryHandler(
        IBookRepository books,
        ILoanRepository loans,
        IFineRepository fines,
        IMemberRepository members,
        TimeProvider time)
    {
        _books = books;
        _loans = loans;
        _fines = fines;
        _members = members;
        _time = time;
    }

    public async Task<Result<DashboardStatisticsDto>> Handle(GetDashboardStatisticsQuery request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var booksPaged = await _books.GetPagedAsync(new PagedRequest(1, 10000), ct: ct);
        var totalBooks = booksPaged.TotalCount;
        var totalCopies = booksPaged.Items.Sum(b => b.TotalCopies);
        var availableCopies = booksPaged.Items.Sum(b => b.AvailableCopies);

        var overdueLoans = await _loans.GetOverdueLoanItemsAsync(now, ct);
        var overdueCount = overdueLoans.Count;

        var allLoans = await _loans.GetPagedAsync(new PagedRequest(1, 10000), ct: ct);
        var activeLoansCount = allLoans.Items.Sum(l => l.Items.Count(i => !i.IsReturned));

        var finesPaged = await _fines.GetPagedAsync(new PagedRequest(1, 10000), null, ct);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyRevenue = finesPaged.Items
            .SelectMany(f => f.Payments)
            .Where(p => !p.IsDeleted && p.PaidAtUtc >= startOfMonth)
            .Sum(p => p.Amount);

        var membersPaged = await _members.GetPagedAsync(new PagedRequest(1, 10000), status: CardStatus.Active, ct: ct);
        var totalActiveMembers = membersPaged.TotalCount;

        return Result.Success(new DashboardStatisticsDto(
            totalBooks,
            totalCopies,
            availableCopies,
            activeLoansCount,
            overdueCount,
            monthlyRevenue,
            totalActiveMembers));
    }
}

// RPT-04: Danh sách sách quá hạn và bạn đọc nợ phạt
public sealed record GetOverdueReportQuery : IRequest<Result<IReadOnlyList<OverdueLoanReportDto>>>;

public sealed class GetOverdueReportQueryHandler : IRequestHandler<GetOverdueReportQuery, Result<IReadOnlyList<OverdueLoanReportDto>>>
{
    private readonly ILoanRepository _loans;
    private readonly TimeProvider _time;

    public GetOverdueReportQueryHandler(ILoanRepository loans, TimeProvider time)
    {
        _loans = loans;
        _time = time;
    }

    public async Task<Result<IReadOnlyList<OverdueLoanReportDto>>> Handle(GetOverdueReportQuery request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var overdueItems = await _loans.GetOverdueLoanItemsAsync(now, ct);

        var list = overdueItems.Select(item =>
        {
            var overdueDays = (int)Math.Ceiling((now - item.DueDateUtc).TotalDays);
            var estimatedFine = overdueDays * BusinessRuleConstants.DailyLateFineRate;
            var coverPrice = item.BookCopy?.Book?.Price ?? 0m;
            if (coverPrice > 0 && estimatedFine > coverPrice)
                estimatedFine = coverPrice;

            return new OverdueLoanReportDto(
                item.Id,
                item.Loan?.Member?.FullName ?? "N/A",
                item.Loan?.Member?.CardNumber ?? "N/A",
                item.Loan?.Member?.ClassOrDepartment ?? "N/A",
                item.Loan?.Member?.PhoneNumber,
                item.BookCopy?.Book?.Title ?? "N/A",
                item.BookCopy?.Barcode ?? "N/A",
                item.Loan?.BorrowDateUtc ?? DateTime.MinValue,
                item.DueDateUtc,
                overdueDays,
                estimatedFine);
        }).ToList();

        return Result.Success<IReadOnlyList<OverdueLoanReportDto>>(list);
    }
}
