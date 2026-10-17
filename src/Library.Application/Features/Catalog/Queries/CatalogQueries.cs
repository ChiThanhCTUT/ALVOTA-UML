using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Catalog.DTOs;
using Library.Domain.Common;
using MediatR;

namespace Library.Application.Features.Catalog.Queries;

// CAT-08: Tra cứu theo tiêu đề, tác giả, ISBN, mã vạch, thể loại; phân trang và sắp xếp
public sealed record GetBooksQuery(
    PagedRequest Paging,
    Guid? CategoryId = null) : IRequest<Result<PagedResult<BookDto>>>;

public sealed class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, Result<PagedResult<BookDto>>>
{
    private readonly IBookRepository _books;

    public GetBooksQueryHandler(IBookRepository books)
    {
        _books = books;
    }

    public async Task<Result<PagedResult<BookDto>>> Handle(GetBooksQuery request, CancellationToken ct)
    {
        var pagedBooks = await _books.GetPagedAsync(request.Paging, request.CategoryId, ct);
        var dtos = pagedBooks.Items.Select(BookDto.From).ToList();

        var result = new PagedResult<BookDto>(
            dtos,
            pagedBooks.TotalCount,
            pagedBooks.PageIndex,
            pagedBooks.PageSize);

        return Result.Success(result);
    }
}

public sealed record GetBookByIdQuery(Guid Id) : IRequest<Result<BookDto>>;

public sealed class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, Result<BookDto>>
{
    private readonly IBookRepository _books;

    public GetBookByIdQueryHandler(IBookRepository books)
    {
        _books = books;
    }

    public async Task<Result<BookDto>> Handle(GetBookByIdQuery request, CancellationToken ct)
    {
        var book = await _books.GetByIdAsync(request.Id, ct);
        if (book is null) return Result.Failure<BookDto>(DomainErrors.Catalog.BookNotFound);
        return Result.Success(BookDto.From(book));
    }
}

public sealed record GetCatalogLookupsQuery : IRequest<Result<(IReadOnlyList<LookupItemDto> Categories, IReadOnlyList<LookupItemDto> Authors, IReadOnlyList<LookupItemDto> Publishers)>>;

public sealed class GetCatalogLookupsQueryHandler : IRequestHandler<GetCatalogLookupsQuery, Result<(IReadOnlyList<LookupItemDto> Categories, IReadOnlyList<LookupItemDto> Authors, IReadOnlyList<LookupItemDto> Publishers)>>
{
    private readonly ILookupRepository _lookups;

    public GetCatalogLookupsQueryHandler(ILookupRepository lookups)
    {
        _lookups = lookups;
    }

    public async Task<Result<(IReadOnlyList<LookupItemDto> Categories, IReadOnlyList<LookupItemDto> Authors, IReadOnlyList<LookupItemDto> Publishers)>> Handle(GetCatalogLookupsQuery request, CancellationToken ct)
    {
        var categories = (await _lookups.GetCategoriesAsync(ct))
            .Select(c => new LookupItemDto(c.Id, c.Name, c.Description)).ToList();

        var authors = (await _lookups.GetAuthorsAsync(ct))
            .Select(a => new LookupItemDto(a.Id, a.FullName, a.Biography)).ToList();

        var publishers = (await _lookups.GetPublishersAsync(ct))
            .Select(p => new LookupItemDto(p.Id, p.Name, p.Address)).ToList();

        return Result.Success(((IReadOnlyList<LookupItemDto>)categories, (IReadOnlyList<LookupItemDto>)authors, (IReadOnlyList<LookupItemDto>)publishers));
    }
}
