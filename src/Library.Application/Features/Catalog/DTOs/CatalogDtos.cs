using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Catalog.DTOs;

public record BookCopyDto(
    Guid Id,
    Guid BookId,
    string BookTitle,
    string Barcode,
    string ShelfLocation,
    CopyStatus Status,
    string? ConditionNote)
{
    public static BookCopyDto From(BookCopy copy) => new(
        copy.Id,
        copy.BookId,
        copy.Book?.Title ?? string.Empty,
        copy.Barcode,
        copy.ShelfLocation,
        copy.Status,
        copy.ConditionNote);
}

public record BookDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string? Isbn,
    int? PublicationYear,
    Guid CategoryId,
    string CategoryName,
    Guid PublisherId,
    string PublisherName,
    string Language,
    int PageCount,
    decimal Price,
    string? CoverImagePath,
    string? Summary,
    bool IsReferenceOnly,
    IReadOnlyList<string> Authors,
    int TotalCopies,
    int AvailableCopies,
    IReadOnlyList<BookCopyDto> Copies)
{
    public static BookDto From(Book book) => new(
        book.Id,
        book.Title,
        book.Subtitle,
        book.Isbn,
        book.PublicationYear,
        book.CategoryId,
        book.Category?.Name ?? string.Empty,
        book.PublisherId,
        book.Publisher?.Name ?? string.Empty,
        book.Language,
        book.PageCount,
        book.Price,
        book.CoverImagePath,
        book.Summary,
        book.IsReferenceOnly,
        book.BookAuthors
            .Where(ba => ba.Author is not null && !string.IsNullOrWhiteSpace(ba.Author.FullName))
            .Select(ba => ba.Author.FullName)
            .ToList(),
        book.TotalCopies,
        book.AvailableCopies,
        book.Copies.Where(c => !c.IsDeleted).Select(BookCopyDto.From).ToList());
}

public record LookupItemDto(Guid Id, string Name, string? Description);
