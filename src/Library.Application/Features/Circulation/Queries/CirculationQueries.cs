using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.DTOs;
using Library.Domain.Common;
using MediatR;

namespace Library.Application.Features.Circulation.Queries;

// CIR-07: Màn hình danh sách quá hạn, đánh dấu màu theo số ngày trễ
public sealed record GetOverdueLoansQuery : IRequest<Result<IReadOnlyList<LoanItemDto>>>;

public sealed class GetOverdueLoansQueryHandler : IRequestHandler<GetOverdueLoansQuery, Result<IReadOnlyList<LoanItemDto>>>
{
    private readonly ILoanRepository _loans;
    private readonly TimeProvider _time;

    public GetOverdueLoansQueryHandler(ILoanRepository loans, TimeProvider time)
    {
        _loans = loans;
        _time = time;
    }

    public async Task<Result<IReadOnlyList<LoanItemDto>>> Handle(GetOverdueLoansQuery request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var overdueItems = await _loans.GetOverdueLoanItemsAsync(now, ct);

        var dtos = overdueItems
            .OrderByDescending(i => now - i.DueDateUtc)
            .Select(i => LoanItemDto.From(i, now))
            .ToList();

        return Result.Success<IReadOnlyList<LoanItemDto>>(dtos);
    }
}

public sealed record GetActiveLoansByMemberQuery(Guid MemberId) : IRequest<Result<IReadOnlyList<LoanItemDto>>>;

public sealed class GetActiveLoansByMemberQueryHandler : IRequestHandler<GetActiveLoansByMemberQuery, Result<IReadOnlyList<LoanItemDto>>>
{
    private readonly ILoanRepository _loans;
    private readonly TimeProvider _time;

    public GetActiveLoansByMemberQueryHandler(ILoanRepository loans, TimeProvider time)
    {
        _loans = loans;
        _time = time;
    }

    public async Task<Result<IReadOnlyList<LoanItemDto>>> Handle(GetActiveLoansByMemberQuery request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var items = await _loans.GetActiveLoanItemsByMemberIdAsync(request.MemberId, ct);
        var dtos = items.Select(i => LoanItemDto.From(i, now)).ToList();
        return Result.Success<IReadOnlyList<LoanItemDto>>(dtos);
    }
}
