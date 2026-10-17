using Library.Domain.Common;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanItem> LoanItems => Set<LoanItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Fine> Fines => Set<Fine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventorySession> InventorySessions => Set<InventorySession>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0)
                {
                    entry.Property(nameof(AuditableEntity.RowVersion)).CurrentValue = Guid.NewGuid().ToByteArray();
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(AuditableEntity.RowVersion)).CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var msg = string.Join("; ", ex.Entries.Select(e => $"{e.Metadata.Name} ({e.State})"));
            throw new InvalidOperationException($"Concurrency failure on: {msg}", ex);
        }
    }
}
