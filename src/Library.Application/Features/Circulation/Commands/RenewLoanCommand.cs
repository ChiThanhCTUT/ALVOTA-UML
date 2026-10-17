using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.DTOs;
using Library.Domain.Common;
using Library.Domain.Enums;
using Library.Domain.Services;
using MediatR;

namespace Library.Application.Features.Circulation.Commands.RenewLoan;

// CIR-05, BR-04
public sealed record RenewLoanCommand(Guid LoanItemId) : IRequest<Result<LoanItemDto>>;

public sealed class RenewLoanCommandValidator : AbstractValidator<RenewLoanCommand>
{
    public RenewLoanCommandValidator()
    {
        RuleFor(x => x.LoanItemId).NotEmpty().WithMessage("Mã lượt mượn không được để trống.");
    }
}

public sealed class RenewLoanCommandHandler : IRequestHandler<RenewLoanCommand, Result<LoanItemDto>>
{
    private readonly ILoanRepository _loans;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public RenewLoanCommandHandler(
        ILoanRepository loans,
        IReservationRepository reservations,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _loans = loans;
        _reservations = reservations;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<LoanItemDto>> Handle(RenewLoanCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var loan = await _loans.GetByIdAsync(request.LoanItemId, ct);
        var loanItem = loan?.Items.FirstOrDefault(i => i.Id == request.LoanItemId);

        if (loanItem is null)
            return Result.Failure<LoanItemDto>(DomainErrors.Circulation.LoanItemNotFound);

        var hasReservation = (await _reservations.GetOldestPendingReservationForBookAsync(loanItem.BookCopy!.BookId, ct)) != null;

        var canRenew = CirculationPolicyService.CanRenew(loanItem, now, hasReservation);
        if (canRenew.IsFailure)
            return Result.Failure<LoanItemDto>(canRenew.Error);

        loanItem.Renew(now);
        _loans.UpdateItem(loanItem);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(LoanItemDto.From(loanItem, now));
    }
}
