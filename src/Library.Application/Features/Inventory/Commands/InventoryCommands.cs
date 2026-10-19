using Library.Application.Common.Interfaces;
using Library.Application.Features.Inventory.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Inventory.Commands;

// INV-01: Tạo đợt kiểm kê theo kệ hoặc toàn kho
public sealed record CreateInventorySessionCommand(string Name, string? ShelfLocation) : IRequest<Result<InventorySessionDto>>;

public sealed class CreateInventorySessionCommandHandler : IRequestHandler<CreateInventorySessionCommand, Result<InventorySessionDto>>
{
    private readonly IInventoryRepository _inventory;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CreateInventorySessionCommandHandler(
        IInventoryRepository inventory,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _inventory = inventory;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<InventorySessionDto>> Handle(CreateInventorySessionCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var session = new InventorySession
        {
            Name = request.Name.Trim(),
            ShelfLocation = string.IsNullOrWhiteSpace(request.ShelfLocation) ? null : request.ShelfLocation.Trim(),
            StartedAtUtc = now,
            Status = InventorySessionStatus.InProgress,
            CreatedAtUtc = now,
            CreatedBy = "System"
        };

        await _inventory.AddSessionAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(InventorySessionDto.From(session));
    }
}

// INV-02: Quét mã vạch để đánh dấu đã kiểm
public sealed record ScanInventoryBarcodeCommand(Guid SessionId, string Barcode) : IRequest<Result<InventoryItemDto>>;

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
            existingItem.ResultStatus = InventoryResultStatus.Normal;
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
                ResultStatus = InventoryResultStatus.Normal,
                CreatedAtUtc = now,
                CreatedBy = "InventoryScanner"
            };
            session.Items.Add(existingItem);
            await _inventory.AddItemAsync(existingItem, ct);
        }

        session.TotalScanned = session.Items.Count(i => i.ScannedAtUtc.HasValue);
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

        session.Status = InventorySessionStatus.Completed;
        session.CompletedAtUtc = now;
        session.TotalMissing = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Missing);
        session.TotalDamaged = session.Items.Count(i => i.ResultStatus == InventoryResultStatus.Damaged);

        _inventory.UpdateSession(session);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(InventorySessionDto.From(session));
    }
}
