using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Inventory.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Inventory.Commands;

// INV-01: Tạo đợt kiểm kê theo kệ hoặc toàn kho
public sealed record CreateInventorySessionCommand(string Name, string? ShelfLocation) : IRequest<Result<InventorySessionDto>>;

public sealed class CreateInventorySessionCommandValidator : AbstractValidator<CreateInventorySessionCommand>
{
    public CreateInventorySessionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên đợt kiểm kê không được để trống.");
    }
}

public sealed class CreateInventorySessionCommandHandler : IRequestHandler<CreateInventorySessionCommand, Result<InventorySessionDto>>
{
    private readonly IInventoryRepository _inventory;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly IBookCopyRepository? _copies;
    private readonly ICurrentUserService? _currentUser;

    public CreateInventorySessionCommandHandler(
        IInventoryRepository inventory,
        IUnitOfWork uow,
        TimeProvider time,
        IBookCopyRepository? copies = null,
        ICurrentUserService? currentUser = null)
    {
        _inventory = inventory;
        _uow = uow;
        _time = time;
        _copies = copies;
        _currentUser = currentUser;
    }

    public async Task<Result<InventorySessionDto>> Handle(CreateInventorySessionCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var shelfLocation = string.IsNullOrWhiteSpace(request.ShelfLocation) ? null : request.ShelfLocation.Trim();
        var createdBy = _currentUser?.Username ?? "System";

        var session = new InventorySession
        {
            Name = request.Name.Trim(),
            ShelfLocation = shelfLocation,
            StartedAtUtc = now,
            Status = InventorySessionStatus.InProgress,
            CreatedAtUtc = now,
            CreatedBy = createdBy
        };

        if (_copies is not null)
        {
            var expectedCopies = await _copies.GetForInventoryAsync(shelfLocation, ct);
            foreach (var copy in expectedCopies)
            {
                session.Items.Add(new InventoryItem
                {
                    InventorySessionId = session.Id,
                    BookCopyId = copy.Id,
                    ScannedBarcode = copy.Barcode,
                    ScannedAtUtc = null,
                    SystemStatus = copy.Status,
                    ScannedStatus = null,
                    ResultStatus = copy.Status == CopyStatus.Damaged
                        ? InventoryResultStatus.Damaged
                        : InventoryResultStatus.Missing,
                    CreatedAtUtc = now,
                    CreatedBy = createdBy
                });
            }

            session.TotalExpected = session.Items.Count;
            session.TotalMissing = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Missing);
            session.TotalDamaged = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Damaged);
        }

        await _inventory.AddSessionAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(InventorySessionDto.From(session));
    }
}

// INV-02: Quét mã vạch để đánh dấu đã kiểm
public sealed record ScanInventoryBarcodeCommand(Guid SessionId, string Barcode) : IRequest<Result<InventoryItemDto>>;

public sealed class ScanInventoryBarcodeCommandValidator : AbstractValidator<ScanInventoryBarcodeCommand>
{
    public ScanInventoryBarcodeCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Barcode).NotEmpty().WithMessage("Mã vạch quét không được để trống.");
    }
}

public sealed class ScanInventoryBarcodeCommandHandler : IRequestHandler<ScanInventoryBarcodeCommand, Result<InventoryItemDto>>
{
    private readonly IInventoryRepository _inventory;
    private readonly IBookCopyRepository _copies;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ScanInventoryBarcodeCommandHandler(
        IInventoryRepository inventory,
        IBookCopyRepository copies,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _inventory = inventory;
        _copies = copies;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<InventoryItemDto>> Handle(ScanInventoryBarcodeCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var session = await _inventory.GetByIdAsync(request.SessionId, ct);
        if (session is null) return Result.Failure<InventoryItemDto>(DomainErrors.Inventory.SessionNotFound);
        if (session.Status != InventorySessionStatus.InProgress)
            return Result.Failure<InventoryItemDto>(DomainErrors.Inventory.SessionClosed);

        var copy = await _copies.GetByBarcodeAsync(request.Barcode.Trim(), ct);
        if (copy is null) return Result.Failure<InventoryItemDto>(DomainErrors.Catalog.CopyNotFound);

        var existingItem = session.Items.FirstOrDefault(i => i.BookCopyId == copy.Id);
        if (existingItem != null)
        {
            existingItem.ScannedAtUtc = now;
            existingItem.ScannedStatus = copy.Status;
            existingItem.ResultStatus = copy.Status == CopyStatus.Damaged
                ? InventoryResultStatus.Damaged
                : InventoryResultStatus.Normal;
            existingItem.UpdatedAtUtc = now;
            _inventory.UpdateItem(existingItem);
        }
        else
        {
            existingItem = new InventoryItem
            {
                InventorySessionId = session.Id,
                BookCopyId = copy.Id,
                BookCopy = copy,
                ScannedBarcode = copy.Barcode,
                ScannedAtUtc = now,
                SystemStatus = copy.Status,
                ScannedStatus = copy.Status,
                ResultStatus = copy.Status == CopyStatus.Damaged
                    ? InventoryResultStatus.Damaged
                    : InventoryResultStatus.Found,
                CreatedAtUtc = now,
                CreatedBy = "InventoryScanner"
            };
            session.Items.Add(existingItem);
            await _inventory.AddItemAsync(existingItem, ct);
        }

        session.TotalScanned = session.Items.Count(i => i.ScannedAtUtc.HasValue);
        session.TotalMissing = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Missing);
        session.TotalDamaged = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Damaged);
        _inventory.UpdateSession(session);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(InventoryItemDto.From(existingItem));
    }
}

// INV-03: Hoàn tất đợt kiểm kê, cập nhật trạng thái
public sealed record CompleteInventorySessionCommand(Guid SessionId) : IRequest<Result<InventorySessionDto>>;

public sealed class CompleteInventorySessionCommandHandler : IRequestHandler<CompleteInventorySessionCommand, Result<InventorySessionDto>>
{
    private readonly IInventoryRepository _inventory;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CompleteInventorySessionCommandHandler(IInventoryRepository inventory, IUnitOfWork uow, TimeProvider time)
    {
        _inventory = inventory;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<InventorySessionDto>> Handle(CompleteInventorySessionCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var session = await _inventory.GetByIdAsync(request.SessionId, ct);
        if (session is null) return Result.Failure<InventorySessionDto>(DomainErrors.Inventory.SessionNotFound);
        if (session.Status != InventorySessionStatus.InProgress)
            return Result.Failure<InventorySessionDto>(DomainErrors.Inventory.SessionClosed);

        foreach (var item in session.Items.Where(i => !i.ScannedAtUtc.HasValue && i.ResultStatus != InventoryResultStatus.Damaged))
        {
            item.ResultStatus = InventoryResultStatus.Missing;
            item.UpdatedAtUtc = now;
            _inventory.UpdateItem(item);
        }

        session.Status = InventorySessionStatus.Completed;
        session.CompletedAtUtc = now;
        session.TotalScanned = session.Items.Count(i => i.ScannedAtUtc.HasValue);
        session.TotalMissing = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Missing);
        session.TotalDamaged = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Damaged);

        _inventory.UpdateSession(session);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(InventorySessionDto.From(session));
    }
}
