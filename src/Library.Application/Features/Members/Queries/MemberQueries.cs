using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Circulation.DTOs;
using Library.Application.Features.Fines.DTOs;
using Library.Application.Features.Members.DTOs;
using Library.Domain.Common;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Members.Queries;

// MEM-06: Phân trang và tìm kiếm bạn đọc
public sealed record GetMembersQuery(
    PagedRequest Paging,
    MemberType? Type = null,
    CardStatus? Status = null) : IRequest<Result<PagedResult<MemberDto>>>;

public sealed class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, Result<PagedResult<MemberDto>>>
{
    private readonly IMemberRepository _members;
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;

    public GetMembersQueryHandler(IMemberRepository members, ILoanRepository loans, IFineRepository fines)
    {
        _members = members;
        _loans = loans;
        _fines = fines;
    }

    public async Task<Result<PagedResult<MemberDto>>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        var pagedMembers = await _members.GetPagedAsync(request.Paging, request.Type, request.Status, ct);

        var dtoList = new List<MemberDto>();
        foreach (var m in pagedMembers.Items)
        {
            var activeLoans = (await _loans.GetActiveLoanItemsByMemberIdAsync(m.Id, ct)).Count;
            var unpaidFines = (await _fines.GetUnpaidByMemberIdAsync(m.Id, ct)).Sum(f => f.RemainingAmount);
            dtoList.Add(MemberDto.From(m, activeLoans, unpaidFines));
        }

        var result = new PagedResult<MemberDto>(
            dtoList,
            pagedMembers.TotalCount,
            pagedMembers.PageIndex,
            pagedMembers.PageSize);

        return Result.Success(result);
    }
}

public sealed record GetMemberByIdQuery(Guid Id) : IRequest<Result<MemberDto>>;

public sealed class GetMemberByIdQueryHandler : IRequestHandler<GetMemberByIdQuery, Result<MemberDto>>
{
    private readonly IMemberRepository _members;
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;

    public GetMemberByIdQueryHandler(IMemberRepository members, ILoanRepository loans, IFineRepository fines)
    {
        _members = members;
        _loans = loans;
        _fines = fines;
    }

    public async Task<Result<MemberDto>> Handle(GetMemberByIdQuery request, CancellationToken ct)
    {
        var member = await _members.GetByIdAsync(request.Id, ct);
        if (member is null) return Result.Failure<MemberDto>(DomainErrors.Member.NotFound);

        var activeLoans = (await _loans.GetActiveLoanItemsByMemberIdAsync(member.Id, ct)).Count;
        var unpaidFines = (await _fines.GetUnpaidByMemberIdAsync(member.Id, ct)).Sum(f => f.RemainingAmount);

        return Result.Success(MemberDto.From(member, activeLoans, unpaidFines));
    }
}

public sealed record GetMemberByCardNumberQuery(string CardNumber) : IRequest<Result<MemberDto>>;

public sealed class GetMemberByCardNumberQueryHandler : IRequestHandler<GetMemberByCardNumberQuery, Result<MemberDto>>
{
    private readonly IMemberRepository _members;
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;

    public GetMemberByCardNumberQueryHandler(IMemberRepository members, ILoanRepository loans, IFineRepository fines)
    {
        _members = members;
        _loans = loans;
        _fines = fines;
    }

    public async Task<Result<MemberDto>> Handle(GetMemberByCardNumberQuery request, CancellationToken ct)
    {
        var member = await _members.GetByCardNumberAsync(request.CardNumber.Trim(), ct);
        if (member is null) return Result.Failure<MemberDto>(DomainErrors.Member.NotFound);

        var activeLoans = (await _loans.GetActiveLoanItemsByMemberIdAsync(member.Id, ct)).Count;
        var unpaidFines = (await _fines.GetUnpaidByMemberIdAsync(member.Id, ct)).Sum(f => f.RemainingAmount);

        return Result.Success(MemberDto.From(member, activeLoans, unpaidFines));
    }
}

// MEM-06: Xem lịch sử mượn trả, phiếu phạt, sách đang mượn của từng bạn đọc
public record MemberDetailHistoryDto(
    MemberDto Member,
    IReadOnlyList<LoanItemDto> ActiveLoans,
    IReadOnlyList<LoanItemDto> BorrowHistory,
    IReadOnlyList<FineDto> Fines);

public sealed record GetMemberDetailHistoryQuery(Guid MemberId) : IRequest<Result<MemberDetailHistoryDto>>;

public sealed class GetMemberDetailHistoryQueryHandler : IRequestHandler<GetMemberDetailHistoryQuery, Result<MemberDetailHistoryDto>>
{
    private readonly IMemberRepository _members;
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;
    private readonly TimeProvider _time;

    public GetMemberDetailHistoryQueryHandler(
        IMemberRepository members,
        ILoanRepository loans,
        IFineRepository fines,
        TimeProvider time)
    {
        _members = members;
        _loans = loans;
        _fines = fines;
        _time = time;
    }

    public async Task<Result<MemberDetailHistoryDto>> Handle(GetMemberDetailHistoryQuery request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var member = await _members.GetByIdAsync(request.MemberId, ct);
        if (member is null) return Result.Failure<MemberDetailHistoryDto>(DomainErrors.Member.NotFound);

        var activeLoans = (await _loans.GetActiveLoanItemsByMemberIdAsync(member.Id, ct))
            .Select(i => LoanItemDto.From(i, now)).ToList();

        var fines = (await _fines.GetUnpaidByMemberIdAsync(member.Id, ct))
            .Select(FineDto.From).ToList();

        var memberDto = MemberDto.From(member, activeLoans.Count, fines.Sum(f => f.RemainingAmount));

        return Result.Success(new MemberDetailHistoryDto(memberDto, activeLoans, activeLoans, fines));
    }
}
