using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Services;
using MediatR;

namespace Library.Application.Features.Circulation.Commands.ReturnBook;

// CIR-03, CIR-04, CIR-09, BR-05, BR-06, BR-07, BR-08, BR-10, FIN-01, FIN-02, FIN-05
public sealed record ReturnBookCommand(
    string Barcode,
    ReturnCondition Condition = ReturnCondition.Normal,
    string? Notes = null) : IRequest<Result<ReturnResultDto>>;

public sealed class ReturnBookCommandValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookCommandValidator()
    {
        RuleFor(x => x.Barcode).NotEmpty().WithMessage("Mã vạch bản sao không được để trống.");
        RuleFor(x => x.Condition).IsInEnum().WithMessage("Tình trạng sách khi trả không hợp lệ.");
    }
}

public sealed class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Result<ReturnResultDto>>
{
    private readonly ILoanRepository _loans;
    private readonly IBookCopyRepository _copies;
    private readonly IFineRepository _fines;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ReturnBookCommandHandler(
        ILoanRepository loans,
        IBookCopyRepository copies,
        IFineRepository fines,
        IReservationRepository reservations,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _loans = loans;
        _copies = copies;
        _fines = fines;
        _reservations = reservations;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<ReturnResultDto>> Handle(ReturnBookCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var loanItem = await _loans.GetActiveLoanItemByCopyBarcodeAsync(request.Barcode.Trim(), ct);
        if (loanItem is null)
            return Result.Failure<ReturnResultDto>(DomainErrors.Circulation.LoanItemNotFound);

        if (loanItem.IsReturned)
            return Result.Failure<ReturnResultDto>(DomainErrors.Circulation.AlreadyReturned);

        var copy = loanItem.BookCopy ?? await _copies.GetByIdAsync(loanItem.BookCopyId, ct);
        if (copy is null)
            return Result.Failure<ReturnResultDto>(DomainErrors.Catalog.CopyNotFound);

        loanItem.MarkReturned(now, request.Condition);

        var coverPrice = copy.Book?.Price ?? 0m;
        var totalFine = 0m;
        FineReason? primaryReason = null;
        var fineDescription = string.Empty;

        // Overdue check (BR-05, BR-06)
        var overdueDays = (now > loanItem.DueDateUtc) ? (int)Math.Ceiling((now - loanItem.DueDateUtc).TotalDays) : 0;
        if (overdueDays > 0)
        {
            var overdueFine = FineCalculator.CalculateOverdueFine(loanItem.DueDateUtc, now, coverPrice);
            totalFine += overdueFine;
            primaryReason = FineReason.Overdue;
            fineDescription = $"Quá hạn {overdueDays} ngày ({overdueFine:N0} đ)";
        }

        // Damage / Lost check (BR-07, BR-08)
        if (request.Condition != ReturnCondition.Normal)
        {
            var damageFine = request.Condition switch
            {
                ReturnCondition.Lost => FineCalculator.CalculateLostFine(coverPrice),
                _ => FineCalculator.CalculateDamagedFine(coverPrice, request.Condition)
            };

            totalFine += damageFine;
            primaryReason ??= request.Condition switch
            {
                ReturnCondition.Lost => FineReason.Lost,
                ReturnCondition.DamagedHeavy => FineReason.DamagedHeavy,
                _ => FineReason.DamagedLight
            };

            fineDescription += (string.IsNullOrEmpty(fineDescription) ? string.Empty : "; ") +
                               $"Phạt tình trạng {request.Condition}: {damageFine:N0} đ";
        }

        Guid? createdFineId = null;
        if (totalFine > 0 && primaryReason.HasValue && loanItem.FineId == null) // FIN-05: avoid duplicate fines
        {
            var fine = new Fine
            {
                MemberId = loanItem.Loan!.MemberId,
                LoanItemId = loanItem.Id,
                Amount = totalFine,
                Reason = primaryReason.Value,
                Status = FineStatus.Pending,
                Description = fineDescription,
                CreatedAtUtc = now,
                CreatedBy = "System"
            };

            await _fines.AddAsync(fine, ct);
            loanItem.Fine = fine;
            loanItem.FineId = fine.Id;
            createdFineId = fine.Id;
        }

        copy.MarkReturned(request.Condition);

        // Reservation queue handling (BR-10)
        var hasReservationNotified = false;
        if (copy.Status == CopyStatus.Available)
        {
            var pendingRes = await _reservations.GetOldestPendingReservationForBookAsync(copy.BookId, ct);
            if (pendingRes != null)
            {
                pendingRes.NotifyAvailable(copy, now);
                copy.MarkReserved();
                _reservations.Update(pendingRes);
                hasReservationNotified = true;
            }
        }

        _loans.UpdateItem(loanItem);
        _copies.Update(copy);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new ReturnResultDto(
            loanItem.Id,
            copy.Barcode,
            copy.Book?.Title ?? string.Empty,
            loanItem.Loan?.Member?.FullName ?? string.Empty,
            request.Condition,
            overdueDays,
            totalFine,
            createdFineId,
            hasReservationNotified));
    }
}
