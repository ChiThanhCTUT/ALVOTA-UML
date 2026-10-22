using System.Globalization;
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
    private readonly ISettingRepository? _settings;

    public RenewLoanCommandHandler(
        ILoanRepository loans,
        IReservationRepository reservations,
        IUnitOfWork uow,
        TimeProvider time,
        ISettingRepository? settings = null)
    {
        _loans = loans;
        _reservations = reservations;
        _uow = uow;
        _time = time;
        _settings = settings;
    }

    public async Task<Result<LoanItemDto>> Handle(RenewLoanCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var loan = await _loans.GetByIdAsync(request.LoanItemId, ct);
        var loanItem = loan?.Items.FirstOrDefault(i => i.Id == request.LoanItemId);

        if (loanItem is null)
            return Result.Failure<LoanItemDto>(DomainErrors.Circulation.LoanItemNotFound);

        var hasReservation = (await _reservations.GetOldestPendingReservationForBookAsync(loanItem.BookCopy!.BookId, ct)) != null;

        var maxRenewalCount = await GetIntSettingAsync("MaxRenewalCount", BusinessRuleConstants.MaxRenewalCount, ct);
        var renewalDays = await GetIntSettingAsync("RenewalDays", BusinessRuleConstants.RenewalDays, ct);

        var canRenew = CirculationPolicyService.CanRenew(loanItem, now, hasReservation, maxRenewalCount);
        if (canRenew.IsFailure)
            return Result.Failure<LoanItemDto>(canRenew.Error);

        loanItem.Renew(now, renewalDays);
        _loans.UpdateItem(loanItem);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(LoanItemDto.From(loanItem, now));
    }

    private async Task<int> GetIntSettingAsync(string key, int defaultValue, CancellationToken ct)
    {
        if (_settings is null) return defaultValue;
        var raw = await _settings.GetValueOrDefaultAsync(key, defaultValue.ToString(CultureInfo.InvariantCulture), ct);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}
