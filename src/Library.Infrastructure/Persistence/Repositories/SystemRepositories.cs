using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly LibraryDbContext _context;

    public InventoryRepository(LibraryDbContext context) => _context = context;

    public async Task<InventorySession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.InventorySessions
            .Include(s => s.Items).ThenInclude(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<PagedResult<InventorySession>> GetPagedSessionsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _context.InventorySessions.AsNoTracking().AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.StartedAtUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<InventorySession>(items, total, request.PageIndex, request.PageSize);
    }

    public async Task AddSessionAsync(InventorySession session, CancellationToken ct = default) => await _context.InventorySessions.AddAsync(session, ct);
    public void UpdateSession(InventorySession session) => _context.InventorySessions.Update(session);
    public async Task AddItemAsync(InventoryItem item, CancellationToken ct = default) => await _context.InventoryItems.AddAsync(item, ct);
    public void UpdateItem(InventoryItem item) => _context.InventoryItems.Update(item);
}

public sealed class UserRepository : IUserRepository
{
    private readonly LibraryDbContext _context;

    public UserRepository(LibraryDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username, ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Users.Include(u => u.Role).OrderBy(u => u.Username).ToListAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default) => await _context.Users.AddAsync(user, ct);
    public void Update(User user) => _context.Users.Update(user);

    public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _context.Roles.ToListAsync(ct);
    }
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly LibraryDbContext _context;

    public AuditLogRepository(LibraryDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
    {
        await _context.AuditLogs.AddAsync(log, ct);
    }

    public async Task<PagedResult<AuditLog>> GetPagedAsync(PagedRequest request, string? entityName = null, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Username.ToLower().Contains(term) ||
                a.Action.ToLower().Contains(term) ||
                (a.Details != null && a.Details.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLog>(items, total, request.PageIndex, request.PageSize);
    }
}

public sealed class SettingRepository : ISettingRepository
{
    private readonly LibraryDbContext _context;

    public SettingRepository(LibraryDbContext context) => _context = context;

    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await _context.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);
    }

    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Settings.AsNoTracking().ToListAsync(ct);
    }

    public async Task<string> GetValueOrDefaultAsync(string key, string defaultValue, CancellationToken ct = default)
    {
        var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value ?? defaultValue;
    }

    public async Task SetValueAsync(string key, string value, string category, string? description = null, CancellationToken ct = default)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "System"
            };
            await _context.Settings.AddAsync(setting, ct);
        }
        else
        {
            setting.Value = value;
            setting.Category = category;
            if (description != null) setting.Description = description;
            setting.UpdatedAtUtc = DateTime.UtcNow;
            _context.Settings.Update(setting);
        }
    }
}

public sealed class LookupRepository : ILookupRepository
{
    private readonly LibraryDbContext _context;

    public LookupRepository(LibraryDbContext context) => _context = context;

    public async Task<IReadOnlyList<Author>> GetAuthorsAsync(CancellationToken ct = default) =>
        await _context.Authors.AsNoTracking().OrderBy(a => a.FullName).ToListAsync(ct);

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Publisher>> GetPublishersAsync(CancellationToken ct = default) =>
        await _context.Publishers.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<Author> GetOrCreateAuthorAsync(string name, CancellationToken ct = default)
    {
        var cleanName = name.Trim();
        var existing = await _context.Authors.FirstOrDefaultAsync(a => a.FullName.ToLower() == cleanName.ToLower(), ct);
        if (existing != null) return existing;

        var author = new Author { FullName = cleanName, CreatedAtUtc = DateTime.UtcNow };
        await _context.Authors.AddAsync(author, ct);
        await _context.SaveChangesAsync(ct);
        return author;
    }

    public async Task<Category> GetOrCreateCategoryAsync(string name, CancellationToken ct = default)
    {
        var cleanName = name.Trim();
        var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == cleanName.ToLower(), ct);
        if (existing != null) return existing;

        var category = new Category { Name = cleanName, CreatedAtUtc = DateTime.UtcNow };
        await _context.Categories.AddAsync(category, ct);
        await _context.SaveChangesAsync(ct);
        return category;
    }

    public async Task<Publisher> GetOrCreatePublisherAsync(string name, CancellationToken ct = default)
    {
        var cleanName = name.Trim();
        var existing = await _context.Publishers.FirstOrDefaultAsync(p => p.Name.ToLower() == cleanName.ToLower(), ct);
        if (existing != null) return existing;

        var publisher = new Publisher { Name = cleanName, CreatedAtUtc = DateTime.UtcNow };
        await _context.Publishers.AddAsync(publisher, ct);
        await _context.SaveChangesAsync(ct);
        return publisher;
    }
}
