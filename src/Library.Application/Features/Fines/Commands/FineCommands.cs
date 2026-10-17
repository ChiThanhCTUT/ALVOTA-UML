using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Fines.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Fines.Commands;

// FIN-03: Ghi nhận thanh toán tiền mặt, in biên lai
public sealed record PayFineCommand(
    Guid FineId,
    decimal Amount,
    string? Notes = null) : IRequest<Result<FinePaymentDto>>;

public sealed class PayFineCommandValidator : AbstractValidator<PayFineCommand>
{
    public PayFineCommandValidator()
    {
        RuleFor(x => x.FineId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền thanh toán phải lớn hơn 0.");
    }
}

public sealed class PayFineCommandHandler : IRequestHandler<PayFineCommand, Result<FinePaymentDto>>
{
    private readonly IFineRepository _fines;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public PayFineCommandHandler(
        IFineRepository fines,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _fines = fines;
        _currentUser = currentUser;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<FinePaymentDto>> Handle(PayFineCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var fine = await _fines.GetByIdAsync(request.FineId, ct);
        if (fine is null) return Result.Failure<FinePaymentDto>(DomainErrors.Fine.NotFound);

        if (fine.Status == FineStatus.Paid || fine.Status == FineStatus.Waived)
            return Result.Failure<FinePaymentDto>(DomainErrors.Fine.AlreadyPaid);

        if (request.Amount > fine.RemainingAmount)
            return Result.Failure<FinePaymentDto>(DomainErrors.Fine.InvalidPaymentAmount);

        var receivedBy = _currentUser.Username ?? "Cashier";
        var receiptNumber = $"BL-{now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";

        var payment = fine.AddPayment(request.Amount, receivedBy, receiptNumber, now, request.Notes);
        await _fines.AddPaymentAsync(payment, ct);

        _fines.Update(fine);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(FinePaymentDto.From(payment));
    }
}

// FIN-04: Miễn/giảm phạt cần quyền Thủ thư trở lên, bắt buộc nhập lý do, ghi audit log
public sealed record WaiveFineCommand(
    Guid FineId,
    string Reason) : IRequest<Result>;

public sealed class WaiveFineCommandValidator : AbstractValidator<WaiveFineCommand>
{
    public WaiveFineCommandValidator()
    {
        RuleFor(x => x.FineId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Bắt buộc nhập lý do khi miễn giảm tiền phạt.");
    }
}

public sealed class WaiveFineCommandHandler : IRequestHandler<WaiveFineCommand, Result>
{
    private readonly IFineRepository _fines;
    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public WaiveFineCommandHandler(
        IFineRepository fines,
        IAuditLogRepository auditLogs,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _fines = fines;
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _uow = uow;
        _time = time;
    }

    public async Task<Result> Handle(WaiveFineCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        // Check role: Librarian or Admin required
        var role = _currentUser.Role;
        if (role != "Admin" && role != "Librarian")
        {
            return Result.Failure(DomainErrors.Auth.Unauthorized);
        }

        var fine = await _fines.GetByIdAsync(request.FineId, ct);
        if (fine is null) return Result.Failure(DomainErrors.Fine.NotFound);

        if (fine.Status == FineStatus.Paid || fine.Status == FineStatus.Waived)
            return Result.Failure(DomainErrors.Fine.AlreadyPaid);

        var waivedBy = _currentUser.Username ?? "Admin";
        fine.Waive(waivedBy, request.Reason.Trim(), now);

        // Ghi Audit Log bắt buộc theo FIN-04, SEC-07
        var audit = new AuditLog
        {
            UserId = Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null,
            Username = waivedBy,
            Action = "WaiveFine",
            EntityName = nameof(Fine),
            EntityId = fine.Id.ToString(),
            Details = $"Miễn phạt số tiền {fine.Amount:N0} đ. Lý do: {request.Reason}",
            TimestampUtc = now
        };

        await _auditLogs.AddAsync(audit, ct);
        _fines.Update(fine);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
