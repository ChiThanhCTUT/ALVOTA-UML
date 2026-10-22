using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context) => _context = context;

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken ct = default)
    {
        return await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Isbn == isbn, ct);
    }

    public async Task<PagedResult<Book>> GetPagedAsync(PagedRequest request, Guid? categoryId = null, CancellationToken ct = default)
    {
        var query = _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Copies)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            var allCandidates = await query
                .OrderByDescending(b => b.CreatedAtUtc)
                .ToListAsync(ct);

            var filtered = allCandidates.Where(b =>
                b.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                (b.Isbn != null && b.Isbn.Contains(term, StringComparison.CurrentCultureIgnoreCase)) ||
                b.BookAuthors.Any(ba => ba.Author != null && ba.Author.FullName.Contains(term, StringComparison.CurrentCultureIgnoreCase)) ||
                b.Copies.Any(c => c.Barcode.Contains(term, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();

            var pagedItems = filtered
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<Book>(pagedItems, filtered.Count, request.PageIndex, request.PageSize);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Book>(items, total, request.PageIndex, request.PageSize);
    }

    public async Task AddAsync(Book book, CancellationToken ct = default)
    {
        await _context.Books.AddAsync(book, ct);
    }

    public void Update(Book book)
    {
        if (_context.Entry(book).State == EntityState.Detached)
            _context.Books.Update(book);
    }

    public async Task<bool> ExistsIsbnAsync(string isbn, Guid? excludeBookId = null, CancellationToken ct = default)
    {
        var query = _context.Books.AsNoTracking().Where(b => b.Isbn == isbn);
        if (excludeBookId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookId.Value);
        }
        return await query.AnyAsync(ct);
    }
}

public sealed class BookCopyRepository : IBookCopyRepository
{
    private readonly LibraryDbContext _context;

    public BookCopyRepository(LibraryDbContext context) => _context = context;

    public async Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.BookCopies
            .Include(c => c.Book)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<BookCopy?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        return await _context.BookCopies
            .Include(c => c.Book)
            .FirstOrDefaultAsync(c => c.Barcode == barcode, ct);
    }

    public async Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(Guid bookId, CancellationToken ct = default)
    {
        return await _context.BookCopies
            .AsNoTracking()
            .Where(c => c.BookId == bookId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BookCopy>> GetForInventoryAsync(string? shelfLocation = null, CancellationToken ct = default)
    {
        var query = _context.BookCopies
            .AsNoTracking()
            .Include(c => c.Book)
            .Where(c => c.Status != Domain.Enums.CopyStatus.Disposed);

        if (!string.IsNullOrWhiteSpace(shelfLocation))
        {
            var shelf = shelfLocation.Trim().ToLower();
            query = query.Where(c => c.ShelfLocation.ToLower() == shelf);
        }

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(BookCopy copy, CancellationToken ct = default)
    {
        await _context.BookCopies.AddAsync(copy, ct);
    }

    public async Task AddRangeAsync(IEnumerable<BookCopy> copies, CancellationToken ct = default)
    {
        await _context.BookCopies.AddRangeAsync(copies, ct);
    }

    public void Update(BookCopy copy)
    {
        if (_context.Entry(copy).State == EntityState.Detached)
            _context.BookCopies.Update(copy);
    }

    public async Task<bool> ExistsBarcodeAsync(string barcode, Guid? excludeCopyId = null, CancellationToken ct = default)
    {
        var query = _context.BookCopies.AsNoTracking().Where(c => c.Barcode == barcode);
        if (excludeCopyId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCopyId.Value);
        }
        return await query.AnyAsync(ct);
    }
}
