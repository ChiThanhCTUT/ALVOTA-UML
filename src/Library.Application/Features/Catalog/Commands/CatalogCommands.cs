using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Catalog.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Catalog.Commands;

// CAT-01: Thêm đầu sách
public sealed record CreateBookCommand(
    string Title,
    string? Subtitle,
    string? Isbn,
    int? PublicationYear,
    Guid CategoryId,
    Guid PublisherId,
    string Language,
    int PageCount,
    decimal Price,
    string? CoverImagePath,
    string? Summary,
    bool IsReferenceOnly,
    IReadOnlyList<Guid> AuthorIds,
    int InitialCopiesCount = 0,
    string? InitialShelfLocation = "Kệ A1") : IRequest<Result<BookDto>>;

public sealed class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Tiêu đề sách không được để trống.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Thể loại không được để trống.");
        RuleFor(x => x.PublisherId).NotEmpty().WithMessage("Nhà xuất bản không được để trống.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Giá bìa không được âm.");
    }
}

public sealed class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, Result<BookDto>>
{
    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CreateBookCommandHandler(
        IBookRepository books,
        IBookCopyRepository copies,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _books = books;
        _copies = copies;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<BookDto>> Handle(CreateBookCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            var exists = await _books.ExistsIsbnAsync(request.Isbn.Trim(), ct: ct);
            if (exists) return Result.Failure<BookDto>(DomainErrors.Catalog.DuplicateIsbn);
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle?.Trim(),
            Isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim(),
            PublicationYear = request.PublicationYear,
            CategoryId = request.CategoryId,
            PublisherId = request.PublisherId,
            Language = string.IsNullOrWhiteSpace(request.Language) ? "Tiếng Việt" : request.Language.Trim(),
            PageCount = request.PageCount,
            Price = request.Price,
            CoverImagePath = request.CoverImagePath,
            Summary = request.Summary?.Trim(),
            IsReferenceOnly = request.IsReferenceOnly,
            CreatedAtUtc = now,
            CreatedBy = "System"
        };

        foreach (var authorId in request.AuthorIds.Distinct())
        {
            book.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = authorId });
        }

        // Tạo sẵn các bản sao ban đầu nếu có yêu cầu (CAT-03)
        for (int i = 1; i <= request.InitialCopiesCount; i++)
        {
            var barcode = $"CP-{DateTime.UtcNow.Ticks % 10000000:D7}-{i:D2}";
            var copy = new BookCopy
            {
                BookId = book.Id,
                Book = book,
                Barcode = barcode,
                ShelfLocation = request.InitialShelfLocation ?? "Kệ chung",
                Status = CopyStatus.Available,
                CreatedAtUtc = now,
                CreatedBy = "System"
            };
            book.Copies.Add(copy);
        }

        await _books.AddAsync(book, ct);
        await _uow.SaveChangesAsync(ct);

        var savedBook = await _books.GetByIdAsync(book.Id, ct) ?? book;
        return Result.Success(BookDto.From(savedBook));
    }
}

// CAT-01: Sửa đầu sách
public sealed record UpdateBookCommand(
    Guid Id,
    string Title,
    string? Subtitle,
    string? Isbn,
    int? PublicationYear,
    Guid CategoryId,
    Guid PublisherId,
    string Language,
    int PageCount,
    decimal Price,
    string? CoverImagePath,
    string? Summary,
    bool IsReferenceOnly,
    IReadOnlyList<Guid> AuthorIds) : IRequest<Result<BookDto>>;

public sealed class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, Result<BookDto>>
{
    private readonly IBookRepository _books;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public UpdateBookCommandHandler(IBookRepository books, IUnitOfWork uow, TimeProvider time)
    {
        _books = books;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<BookDto>> Handle(UpdateBookCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var book = await _books.GetByIdAsync(request.Id, ct);
        if (book is null) return Result.Failure<BookDto>(DomainErrors.Catalog.BookNotFound);

        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            var exists = await _books.ExistsIsbnAsync(request.Isbn.Trim(), excludeBookId: request.Id, ct: ct);
            if (exists) return Result.Failure<BookDto>(DomainErrors.Catalog.DuplicateIsbn);
        }

        book.Title = request.Title.Trim();
        book.Subtitle = request.Subtitle?.Trim();
        book.Isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
        book.PublicationYear = request.PublicationYear;
        book.CategoryId = request.CategoryId;
        book.PublisherId = request.PublisherId;
        book.Language = request.Language;
        book.PageCount = request.PageCount;
        book.Price = request.Price;
        book.CoverImagePath = request.CoverImagePath;
        book.Summary = request.Summary?.Trim();
        book.IsReferenceOnly = request.IsReferenceOnly;
        book.UpdatedAtUtc = now;

        book.BookAuthors.Clear();
        foreach (var authorId in request.AuthorIds.Distinct())
        {
            book.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = authorId });
        }

        _books.Update(book);
        await _uow.SaveChangesAsync(ct);

        var savedBook = await _books.GetByIdAsync(book.Id, ct) ?? book;
        return Result.Success(BookDto.From(savedBook));
    }
}

// CAT-07: Không cho xóa đầu sách còn bản sao đang mượn
public sealed record DeleteBookCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Result>
{
    private readonly IBookRepository _books;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public DeleteBookCommandHandler(IBookRepository books, IUnitOfWork uow, TimeProvider time)
    {
        _books = books;
        _uow = uow;
        _time = time;
    }

    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var book = await _books.GetByIdAsync(request.Id, ct);
        if (book is null) return Result.Failure(DomainErrors.Catalog.BookNotFound);

        if (!book.CanBeDeleted)
            return Result.Failure(DomainErrors.Catalog.HasActiveLoans);

        book.MarkDeleted("System", now);
        foreach (var copy in book.Copies)
        {
            copy.MarkDeleted("System", now);
        }

        _books.Update(book);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// CAT-03: Thêm bản sao cho đầu sách
public sealed record AddBookCopiesCommand(
    Guid BookId,
    int Quantity,
    string ShelfLocation,
    string? BarcodePrefix = "CP") : IRequest<Result<IReadOnlyList<BookCopyDto>>>;

public sealed class AddBookCopiesCommandHandler : IRequestHandler<AddBookCopiesCommand, Result<IReadOnlyList<BookCopyDto>>>
{
    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public AddBookCopiesCommandHandler(
        IBookRepository books,
        IBookCopyRepository copies,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _books = books;
        _copies = copies;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<IReadOnlyList<BookCopyDto>>> Handle(AddBookCopiesCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var book = await _books.GetByIdAsync(request.BookId, ct);
        if (book is null) return Result.Failure<IReadOnlyList<BookCopyDto>>(DomainErrors.Catalog.BookNotFound);

        var newCopies = new List<BookCopy>();
        var prefix = string.IsNullOrWhiteSpace(request.BarcodePrefix) ? "CP" : request.BarcodePrefix.Trim();

        for (int i = 1; i <= request.Quantity; i++)
        {
            var uniqueBarcode = $"{prefix}-{DateTime.UtcNow.Ticks % 10000000:D7}-{i:D2}";
            var copy = new BookCopy
            {
                BookId = book.Id,
                Book = book,
                Barcode = uniqueBarcode,
                ShelfLocation = request.ShelfLocation.Trim(),
                Status = CopyStatus.Available,
                CreatedAtUtc = now,
                CreatedBy = "System"
            };
            newCopies.Add(copy);
        }

        await _copies.AddRangeAsync(newCopies, ct);
        await _uow.SaveChangesAsync(ct);

        var dtos = newCopies.Select(BookCopyDto.From).ToList();
        return Result.Success<IReadOnlyList<BookCopyDto>>(dtos);
    }
}
