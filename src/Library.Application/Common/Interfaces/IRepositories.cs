using Library.Application.Common.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Common.Interfaces;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken ct = default);
    Task<PagedResult<Book>> GetPagedAsync(PagedRequest request, Guid? categoryId = null, CancellationToken ct = default);
    Task AddAsync(Book book, CancellationToken ct = default);
    void Update(Book book);
    Task<bool> ExistsIsbnAsync(string isbn, Guid? excludeBookId = null, CancellationToken ct = default);
}

public interface IBookCopyRepository
{
    Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookCopy?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(Guid bookId, CancellationToken ct = default);
    Task AddAsync(BookCopy copy, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<BookCopy> copies, CancellationToken ct = default);
    void Update(BookCopy copy);
    Task<bool> ExistsBarcodeAsync(string barcode, Guid? excludeCopyId = null, CancellationToken ct = default);
}

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Member?> GetByCardNumberAsync(string cardNumber, CancellationToken ct = default);
    Task<PagedResult<Member>> GetPagedAsync(PagedRequest request, MemberType? type = null, CardStatus? status = null, CancellationToken ct = default);
    Task AddAsync(Member member, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Member> members, CancellationToken ct = default);
    void Update(Member member);
    Task<bool> ExistsCardNumberAsync(string cardNumber, Guid? excludeMemberId = null, CancellationToken ct = default);
}

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LoanItem?> GetActiveLoanItemByCopyBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<IReadOnlyList<LoanItem>> GetActiveLoanItemsByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<IReadOnlyList<LoanItem>> GetOverdueLoanItemsAsync(DateTime utcNow, CancellationToken ct = default);
    Task<PagedResult<Loan>> GetPagedAsync(PagedRequest request, CancellationToken ct = default);
    Task AddAsync(Loan loan, CancellationToken ct = default);
    void Update(Loan loan);
    void UpdateItem(LoanItem item);
}

public interface IFineRepository
{
    Task<Fine?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Fine>> GetUnpaidByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<PagedResult<Fine>> GetPagedAsync(PagedRequest request, FineStatus? status = null, CancellationToken ct = default);
    Task AddAsync(Fine fine, CancellationToken ct = default);
    Task AddPaymentAsync(Payment payment, CancellationToken ct = default);
    void Update(Fine fine);
}

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Reservation?> GetOldestPendingReservationForBookAsync(Guid bookId, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetActiveByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<PagedResult<Reservation>> GetPagedAsync(PagedRequest request, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    void Update(Reservation reservation);
}

public interface IInventoryRepository
{
    Task<InventorySession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<InventorySession>> GetPagedSessionsAsync(PagedRequest request, CancellationToken ct = default);
    Task AddSessionAsync(InventorySession session, CancellationToken ct = default);
    void UpdateSession(InventorySession session);
    Task AddItemAsync(InventoryItem item, CancellationToken ct = default);
    void UpdateItem(InventoryItem item);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<PagedResult<AuditLog>> GetPagedAsync(PagedRequest request, string? entityName = null, CancellationToken ct = default);
}

public interface ISettingRepository
{
    Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken ct = default);
    Task<string> GetValueOrDefaultAsync(string key, string defaultValue, CancellationToken ct = default);
    Task SetValueAsync(string key, string value, string category, string? description = null, CancellationToken ct = default);
}

public interface ILookupRepository
{
    Task<IReadOnlyList<Author>> GetAuthorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Publisher>> GetPublishersAsync(CancellationToken ct = default);
    Task<Author> GetOrCreateAuthorAsync(string name, CancellationToken ct = default);
    Task<Category> GetOrCreateCategoryAsync(string name, CancellationToken ct = default);
    Task<Publisher> GetOrCreatePublisherAsync(string name, CancellationToken ct = default);
}
