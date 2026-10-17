using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context) => _context = context;

    public async Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Members
            .Include(m => m.Loans).ThenInclude(l => l.Items)
            .Include(m => m.Fines)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Member?> GetByCardNumberAsync(string cardNumber, CancellationToken ct = default)
    {
        return await _context.Members
            .Include(m => m.Loans).ThenInclude(l => l.Items)
            .Include(m => m.Fines)
            .FirstOrDefaultAsync(m => m.CardNumber == cardNumber, ct);
    }

    public async Task<PagedResult<Member>> GetPagedAsync(PagedRequest request, MemberType? type = null, CardStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.Members.AsNoTracking().AsQueryable();

        if (type.HasValue) query = query.Where(m => m.Type == type.Value);
        if (status.HasValue) query = query.Where(m => m.CardStatus == status.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(m =>
                m.FullName.ToLower().Contains(term) ||
                m.CardNumber.ToLower().Contains(term) ||
                m.ClassOrDepartment.ToLower().Contains(term) ||
                (m.PhoneNumber != null && m.PhoneNumber.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Member>(items, total, request.PageIndex, request.PageSize);
    }

    public async Task AddAsync(Member member, CancellationToken ct = default) => await _context.Members.AddAsync(member, ct);
    public async Task AddRangeAsync(IEnumerable<Member> members, CancellationToken ct = default) => await _context.Members.AddRangeAsync(members, ct);
    public void Update(Member member)
    {
        if (_context.Entry(member).State == EntityState.Detached)
            _context.Members.Update(member);
    }

    public async Task<bool> ExistsCardNumberAsync(string cardNumber, Guid? excludeMemberId = null, CancellationToken ct = default)
    {
        var query = _context.Members.AsNoTracking().Where(m => m.CardNumber == cardNumber);
        if (excludeMemberId.HasValue)
        {
            query = query.Where(m => m.Id != excludeMemberId.Value);
        }
        return await query.AnyAsync(ct);
    }
}

public sealed class LoanRepository : ILoanRepository
{
    private readonly LibraryDbContext _context;

    public LoanRepository(LibraryDbContext context) => _context = context;

    public async Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Loans
            .Include(l => l.Member)
            .Include(l => l.Items).ThenInclude(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<LoanItem?> GetActiveLoanItemByCopyBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        return await _context.LoanItems
            .Include(i => i.Loan).ThenInclude(l => l!.Member)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(i => i.BookCopy!.Barcode == barcode && i.ReturnedAtUtc == null, ct);
    }

    public async Task<IReadOnlyList<LoanItem>> GetActiveLoanItemsByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.LoanItems
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .Where(i => i.Loan!.MemberId == memberId && i.ReturnedAtUtc == null)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LoanItem>> GetOverdueLoanItemsAsync(DateTime utcNow, CancellationToken ct = default)
    {
        return await _context.LoanItems
            .Include(i => i.Loan).ThenInclude(l => l!.Member)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .Where(i => i.ReturnedAtUtc == null && i.DueDateUtc < utcNow)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Loan>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _context.Loans
            .AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.Items).ThenInclude(i => i.BookCopy).ThenInclude(c => c!.Book)
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.BorrowDateUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Loan>(items, total, request.PageIndex, request.PageSize);
    }

    public async Task AddAsync(Loan loan, CancellationToken ct = default) => await _context.Loans.AddAsync(loan, ct);
    public void Update(Loan loan)
    {
        if (_context.Entry(loan).State == EntityState.Detached)
            _context.Loans.Update(loan);
    }
    public void UpdateItem(LoanItem item)
    {
        if (_context.Entry(item).State == EntityState.Detached)
            _context.LoanItems.Update(item);
    }
}

public sealed class FineRepository : IFineRepository
{
    private readonly LibraryDbContext _context;

    public FineRepository(LibraryDbContext context) => _context = context;

    public async Task<Fine?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Fines
            .Include(f => f.Member)
            .Include(f => f.LoanItem).ThenInclude(i => i!.BookCopy).ThenInclude(c => c!.Book)
            .Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<IReadOnlyList<Fine>> GetUnpaidByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.Fines
            .Include(f => f.Payments)
            .Where(f => f.MemberId == memberId && f.Status == FineStatus.Pending)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Fine>> GetPagedAsync(PagedRequest request, FineStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.Fines
            .AsNoTracking()
            .Include(f => f.Member)
            .Include(f => f.LoanItem).ThenInclude(i => i!.BookCopy).ThenInclude(c => c!.Book)
            .Include(f => f.Payments)
            .AsQueryable();

        if (status.HasValue) query = query.Where(f => f.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(f =>
                f.Member!.FullName.ToLower().Contains(term) ||
                f.Member.CardNumber.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Fine>(items, total, request.PageIndex, request.PageSize);
    }

    public async Task AddAsync(Fine fine, CancellationToken ct = default) => await _context.Fines.AddAsync(fine, ct);
    public async Task AddPaymentAsync(Payment payment, CancellationToken ct = default) => await _context.Payments.AddAsync(payment, ct);
    public void Update(Fine fine)
    {
        if (_context.Entry(fine).State == EntityState.Detached)
            _context.Fines.Update(fine);
    }
}

public sealed class ReservationRepository : IReservationRepository
{
    private readonly LibraryDbContext _context;

    public ReservationRepository(LibraryDbContext context) => _context = context;

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Include(r => r.Book)
            .Include(r => r.Member)
            .Include(r => r.ReservedCopy)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Reservation?> GetOldestPendingReservationForBookAsync(Guid bookId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Where(r => r.BookId == bookId && r.Status == ReservationStatus.Pending)
            .OrderBy(r => r.ReservedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Reservation>> GetActiveByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Where(r => r.MemberId == memberId && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Notified))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Reservation>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _context.Reservations
            .AsNoTracking()
            .Include(r => r.Book)
            .Include(r => r.Member)
            .Include(r => r.ReservedCopy)
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReservedAtUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Reservation>(items, total, request.PageIndex, request.PageSize);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken ct = default) => await _context.Reservations.AddAsync(reservation, ct);
    public void Update(Reservation reservation) => _context.Reservations.Update(reservation);
}
