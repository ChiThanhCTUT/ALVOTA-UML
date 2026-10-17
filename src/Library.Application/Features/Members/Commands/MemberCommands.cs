using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Members.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Members.Commands;

// MEM-01, MEM-02
public sealed record CreateMemberCommand(
    string CardNumber,
    string FullName,
    DateTime DateOfBirth,
    MemberType Type,
    string ClassOrDepartment,
    string? PhoneNumber,
    string? Email,
    string? PhotoPath,
    int ValidMonths = 12) : IRequest<Result<MemberDto>>;

public sealed class CreateMemberCommandValidator : AbstractValidator<CreateMemberCommand>
{
    public CreateMemberCommandValidator()
    {
        RuleFor(x => x.CardNumber).NotEmpty().WithMessage("Mã thẻ không được để trống.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Họ và tên không được để trống.");
        RuleFor(x => x.ClassOrDepartment).NotEmpty().WithMessage("Lớp hoặc tổ chuyên môn không được để trống.");
    }
}

public sealed class CreateMemberCommandHandler : IRequestHandler<CreateMemberCommand, Result<MemberDto>>
{
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CreateMemberCommandHandler(IMemberRepository members, IUnitOfWork uow, TimeProvider time)
    {
        _members = members;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<MemberDto>> Handle(CreateMemberCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var exists = await _members.ExistsCardNumberAsync(request.CardNumber.Trim(), ct: ct);
        if (exists)
            return Result.Failure<MemberDto>(DomainErrors.Member.DuplicateCardNumber);

        var member = new Member
        {
            CardNumber = request.CardNumber.Trim(),
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Type = request.Type,
            ClassOrDepartment = request.ClassOrDepartment.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            PhotoPath = request.PhotoPath,
            CardStatus = CardStatus.Active,
            CardIssuedAtUtc = now,
            CardExpiresAtUtc = now.AddMonths(request.ValidMonths),
            ReissueCount = 0,
            CreatedAtUtc = now,
            CreatedBy = "System"
        };

        await _members.AddAsync(member, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MemberDto.From(member));
    }
}

// MEM-01: Cập nhật bạn đọc
public sealed record UpdateMemberCommand(
    Guid Id,
    string FullName,
    DateTime DateOfBirth,
    MemberType Type,
    string ClassOrDepartment,
    string? PhoneNumber,
    string? Email,
    string? PhotoPath,
    CardStatus CardStatus,
    DateTime CardExpiresAtUtc) : IRequest<Result<MemberDto>>;

public sealed class UpdateMemberCommandHandler : IRequestHandler<UpdateMemberCommand, Result<MemberDto>>
{
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public UpdateMemberCommandHandler(IMemberRepository members, IUnitOfWork uow, TimeProvider time)
    {
        _members = members;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<MemberDto>> Handle(UpdateMemberCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var member = await _members.GetByIdAsync(request.Id, ct);
        if (member is null) return Result.Failure<MemberDto>(DomainErrors.Member.NotFound);

        member.FullName = request.FullName.Trim();
        member.DateOfBirth = request.DateOfBirth;
        member.Type = request.Type;
        member.ClassOrDepartment = request.ClassOrDepartment.Trim();
        member.PhoneNumber = request.PhoneNumber?.Trim();
        member.Email = request.Email?.Trim();
        member.PhotoPath = request.PhotoPath;
        member.CardStatus = request.CardStatus;
        member.CardExpiresAtUtc = request.CardExpiresAtUtc;
        member.UpdatedAtUtc = now;

        _members.Update(member);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MemberDto.From(member));
    }
}

// MEM-05: Cấp lại thẻ khi mất, lưu lịch sử cấp thẻ
public sealed record ReissueCardCommand(
    Guid MemberId,
    string NewCardNumber,
    int ValidMonths = 12) : IRequest<Result<MemberDto>>;

public sealed class ReissueCardCommandHandler : IRequestHandler<ReissueCardCommand, Result<MemberDto>>
{
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ReissueCardCommandHandler(IMemberRepository members, IUnitOfWork uow, TimeProvider time)
    {
        _members = members;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<MemberDto>> Handle(ReissueCardCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var member = await _members.GetByIdAsync(request.MemberId, ct);
        if (member is null) return Result.Failure<MemberDto>(DomainErrors.Member.NotFound);

        var exists = await _members.ExistsCardNumberAsync(request.NewCardNumber.Trim(), excludeMemberId: request.MemberId, ct: ct);
        if (exists) return Result.Failure<MemberDto>(DomainErrors.Member.DuplicateCardNumber);

        member.ReissueCard(request.NewCardNumber.Trim(), now, now.AddMonths(request.ValidMonths));
        _members.Update(member);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(MemberDto.From(member));
    }
}

// MEM-07: Cuối năm học: chuyển lớp hoặc khóa thẻ học sinh ra trường hàng loạt
public sealed record BatchGraduateMembersCommand(
    string CurrentClass,
    string? NewClass,
    bool LockGraduating) : IRequest<Result<int>>;

public sealed class BatchGraduateMembersCommandHandler : IRequestHandler<BatchGraduateMembersCommand, Result<int>>
{
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public BatchGraduateMembersCommandHandler(IMemberRepository members, IUnitOfWork uow, TimeProvider time)
    {
        _members = members;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<int>> Handle(BatchGraduateMembersCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var paged = await _members.GetPagedAsync(new PagedRequest(1, 10000, SearchTerm: request.CurrentClass), ct: ct);
        var matchingMembers = paged.Items.Where(m => m.ClassOrDepartment.Equals(request.CurrentClass, StringComparison.OrdinalIgnoreCase)).ToList();

        var affected = 0;
        foreach (var m in matchingMembers)
        {
            if (request.LockGraduating)
            {
                m.CardStatus = CardStatus.Expired;
                m.UpdatedAtUtc = now;
                _members.Update(m);
                affected++;
            }
            else if (!string.IsNullOrWhiteSpace(request.NewClass))
            {
                m.ClassOrDepartment = request.NewClass.Trim();
                m.UpdatedAtUtc = now;
                _members.Update(m);
                affected++;
            }
        }

        if (affected > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }

        return Result.Success(affected);
    }
}
