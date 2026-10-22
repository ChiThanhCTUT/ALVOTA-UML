using System.Globalization;
using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Services;
using MediatR;

namespace Library.Application.Features.Circulation.Commands.BorrowBook;

// CIR-01, CIR-02, CIR-09, BR-01, BR-02, BR-03, BR-09, BR-11
public sealed record BorrowBookCommand(Guid MemberId, string CopyBarcode) : IRequest<Result<LoanDto>>;

public sealed class BorrowBookCommandValidator : AbstractValidator<BorrowBookCommand>
{
    public BorrowBookCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty().WithMessage("Mã bạn đọc không được để trống.");
        RuleFor(x => x.CopyBarcode).NotEmpty().WithMessage("Mã vạch bản sao không được để trống.");
    }
}

public sealed class BorrowBookCommandHandler : IRequestHandler<BorrowBookCommand, Result<LoanDto>>
{
    private readonly IMemberRepository _members;
    private readonly IBookCopyRepository _copies;
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly ISettingRepository? _settings;

    public BorrowBookCommandHandler(
        IMemberRepository members,
        IBookCopyRepository copies,
        ILoanRepository loans,
        IFineRepository fines,
        IReservationRepository reservations,
        IUnitOfWork uow,
        TimeProvider time,
        ISettingRepository? settings = null)
    {
        _members = members;
        _copies = copies;
        _loans = loans;
        _fines = fines;
        _reservations = reservations;
        _uow = uow;
        _time = time;
        _settings = settings;
    }

    public async Task<Result<LoanDto>> Handle(BorrowBookCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var member = await _members.GetByIdAsync(request.MemberId, ct);
        if (member is null) return Result.Failure<LoanDto>(DomainErrors.Member.NotFound);

        var copy = await _copies.GetByBarcodeAsync(request.CopyBarcode.Trim(), ct);
        if (copy is null) return Result.Failure<LoanDto>(DomainErrors.Catalog.CopyNotFound);

        // Check if copy is reserved for someone else (CIR-06, BR-10, BUG-005)
        if (copy.Status == CopyStatus.Reserved)
        {
            var activeRes = (await _reservations.GetActiveByMemberIdAsync(member.Id, ct))
                .FirstOrDefault(r => r.BookId == copy.BookId &&
                                     r.Status == ReservationStatus.Notified &&
                                     (r.ReservedCopyId == null || r.ReservedCopyId == copy.Id));

            if (activeRes == null || activeRes.IsExpired(now))
            {
                if (activeRes != null && activeRes.IsExpired(now))
                {
                    activeRes.Expire(now);
                    _reservations.Update(activeRes);
                }

                return Result.Failure<LoanDto>(DomainErrors.Circulation.ReservationConflict);
            }

            activeRes.Fulfill(now);
            copy.MarkAvailable();
            _reservations.Update(activeRes);
        }

        var activeLoans = (await _loans.GetActiveLoanItemsByMemberIdAsync(member.Id, ct)) ?? Array.Empty<LoanItem>();
        var hasOverdue = activeLoans.Any(i => i.DueDateUtc < now);
        var unpaidFines = (await _fines.GetUnpaidByMemberIdAsync(member.Id, ct))?.Sum(f => f.RemainingAmount) ?? 0m;

        var studentLimit = await GetIntSettingAsync("StudentBorrowLimit", BusinessRuleConstants.StudentBorrowLimit, ct);
        var teacherLimit = await GetIntSettingAsync("TeacherBorrowLimit", BusinessRuleConstants.TeacherBorrowLimit, ct);
        var studentDays = await GetIntSettingAsync("StudentLoanDurationDays", BusinessRuleConstants.StudentLoanDurationDays, ct);
        var teacherDays = await GetIntSettingAsync("TeacherLoanDurationDays", BusinessRuleConstants.TeacherLoanDurationDays, ct);
        var debtThreshold = await GetDecimalSettingAsync("DebtThresholdToBlockBorrowing", BusinessRuleConstants.DebtThresholdToBlockBorrowing, ct);

        var canBorrowResult = CirculationPolicyService.CanBorrow(
            member,
            copy,
            activeLoans.Count,
            hasOverdue,
            unpaidFines,
            now,
            debtThreshold,
            studentLimit,
            teacherLimit);

        if (canBorrowResult.IsFailure)
            return Result.Failure<LoanDto>(canBorrowResult.Error);

        var durationDays = member.GetLoanDurationDays(studentDays, teacherDays);
        var dueDate = now.AddDays(durationDays);

        var loan = new Loan
        {
            MemberId = member.Id,
            Member = member,
            BorrowDateUtc = now,
            DueDateUtc = dueDate,
            Status = LoanStatus.Active,
            CreatedAtUtc = now,
            CreatedBy = "System"
        };

        var loanItem = new LoanItem
        {
            LoanId = loan.Id,
            Loan = loan,
            BookCopyId = copy.Id,
            BookCopy = copy,
            DueDateUtc = dueDate,
            RenewalCount = 0,
            CreatedAtUtc = now,
            CreatedBy = "System"
        };

        loan.Items.Add(loanItem);
        copy.MarkBorrowed();

        await _loans.AddAsync(loan, ct);
        _copies.Update(copy);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Concurrency failure", StringComparison.Ordinal))
        {
            return Result.Failure<LoanDto>(DomainErrors.Circulation.ConcurrencyConflict);
        }

        return Result.Success(LoanDto.From(loan, now));
    }

    private async Task<int> GetIntSettingAsync(string key, int defaultValue, CancellationToken ct)
    {
        if (_settings is null) return defaultValue;
        var raw = await _settings.GetValueOrDefaultAsync(key, defaultValue.ToString(CultureInfo.InvariantCulture), ct);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }

    private async Task<decimal> GetDecimalSettingAsync(string key, decimal defaultValue, CancellationToken ct)
    {
        if (_settings is null) return defaultValue;
        var raw = await _settings.GetValueOrDefaultAsync(key, defaultValue.ToString(CultureInfo.InvariantCulture), ct);
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0m
            ? parsed
            : defaultValue;
    }
}
