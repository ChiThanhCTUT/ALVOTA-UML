using Library.Application.Common.Interfaces;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Catalog.Commands;

public record BookImportRow(
    string Title,
    string? Isbn,
    string CategoryName,
    string AuthorName,
    string PublisherName,
    decimal Price,
    int Quantity,
    string ShelfLocation);

public record BookImportResultDto(
    int TotalImported,
    int TotalErrors,
    IReadOnlyList<(int Row, string Error)> Errors);

public sealed record ImportBooksExcelCommand(Stream ExcelStream) : IRequest<Result<BookImportResultDto>>;

public sealed class ImportBooksExcelCommandHandler : IRequestHandler<ImportBooksExcelCommand, Result<BookImportResultDto>>
{
    private readonly IExcelService _excelService;
    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly ILookupRepository _lookups;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ImportBooksExcelCommandHandler(
        IExcelService excelService,
        IBookRepository books,
        IBookCopyRepository copies,
        ILookupRepository lookups,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _excelService = excelService;
        _books = books;
        _copies = copies;
        _lookups = lookups;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<BookImportResultDto>> Handle(ImportBooksExcelCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var (rows, parseErrors) = _excelService.ImportFromExcel<BookImportRow>(request.ExcelStream, dict =>
        {
            if (!dict.TryGetValue("Tiêu đề", out var title) || string.IsNullOrWhiteSpace(title))
                return Result.Failure<BookImportRow>(new Error("ROW_INVALID", "Tiêu đề sách không được để trống."));

            dict.TryGetValue("ISBN", out var isbn);
            dict.TryGetValue("Thể loại", out var cat);
            dict.TryGetValue("Tác giả", out var author);
            dict.TryGetValue("Nhà xuất bản", out var pub);
            dict.TryGetValue("Giá bìa", out var priceStr);
            dict.TryGetValue("Số lượng", out var qtyStr);
            dict.TryGetValue("Vị trí kệ", out var shelf);

            if (!decimal.TryParse(priceStr, out var price)) price = 0m;
            if (!int.TryParse(qtyStr, out var qty) || qty <= 0) qty = 1;

            return Result.Success(new BookImportRow(
                title.Trim(),
                string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim(),
                string.IsNullOrWhiteSpace(cat) ? "Tổng hợp" : cat.Trim(),
                string.IsNullOrWhiteSpace(author) ? "Khuyết danh" : author.Trim(),
                string.IsNullOrWhiteSpace(pub) ? "NXB Giáo Dục" : pub.Trim(),
                price,
                qty,
                string.IsNullOrWhiteSpace(shelf) ? "Kệ chung" : shelf.Trim()));
        });

        var importedCount = 0;
        var errorList = new List<(int Row, string Error)>(parseErrors.Select(e => (e.RowNumber, e.ErrorMessage)));

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowIndex = i + 2; // header is row 1

            try
            {
                if (!string.IsNullOrWhiteSpace(row.Isbn))
                {
                    var exists = await _books.ExistsIsbnAsync(row.Isbn, ct: ct);
                    if (exists)
                    {
                        errorList.Add((rowIndex, $"Trùng ISBN: {row.Isbn}"));
                        continue;
                    }
                }

                var category = await _lookups.GetOrCreateCategoryAsync(row.CategoryName, ct);
                var author = await _lookups.GetOrCreateAuthorAsync(row.AuthorName, ct);
                var publisher = await _lookups.GetOrCreatePublisherAsync(row.PublisherName, ct);

                var book = new Book
                {
                    Title = row.Title,
                    Isbn = row.Isbn,
                    CategoryId = category.Id,
                    Category = category,
                    PublisherId = publisher.Id,
                    Publisher = publisher,
                    Price = row.Price,
                    CreatedAtUtc = now,
                    CreatedBy = "ExcelImport"
                };

                book.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = author.Id });

                for (int c = 1; c <= row.Quantity; c++)
                {
                    var barcode = $"CP-{DateTime.UtcNow.Ticks % 10000000:D7}-{c:D2}";
                    book.Copies.Add(new BookCopy
                    {
                        BookId = book.Id,
                        Book = book,
                        Barcode = barcode,
                        ShelfLocation = row.ShelfLocation,
                        Status = CopyStatus.Available,
                        CreatedAtUtc = now,
                        CreatedBy = "ExcelImport"
                    });
                }

                await _books.AddAsync(book, ct);
                await _uow.SaveChangesAsync(ct);
                importedCount++;
            }
            catch (Exception ex)
            {
                errorList.Add((rowIndex, ex.Message));
            }
        }

        return Result.Success(new BookImportResultDto(importedCount, errorList.Count, errorList));
    }
}
