using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Inventory.DTOs;

public record InventorySessionDto(
    Guid Id,
    string Name,
    string? ShelfLocation,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    InventorySessionStatus Status,
    int TotalExpected,
    int TotalScanned,
    int TotalMissing,
    int TotalDamaged)
{
    public static InventorySessionDto From(InventorySession session) => new(
        session.Id,
        session.Name,
        session.ShelfLocation,
        session.StartedAtUtc,
        session.CompletedAtUtc,
        session.Status,
        session.TotalExpected,
        session.TotalScanned,
        session.TotalMissing,
        session.TotalDamaged);
}

public record InventoryItemDto(
    Guid Id,
    Guid BookCopyId,
    string Barcode,
    string BookTitle,
    string ShelfLocation,
    CopyStatus SystemStatus,
    CopyStatus? ScannedStatus,
    InventoryResultStatus ResultStatus,
    DateTime? ScannedAtUtc,
    string? Notes)
{
    public static InventoryItemDto From(InventoryItem item) => new(
        item.Id,
        item.BookCopyId,
        item.ScannedBarcode,
        item.BookCopy?.Book?.Title ?? string.Empty,
        item.BookCopy?.ShelfLocation ?? string.Empty,
        item.SystemStatus,
        item.ScannedStatus,
        item.ResultStatus,
        item.ScannedAtUtc,
        item.Notes);
}
