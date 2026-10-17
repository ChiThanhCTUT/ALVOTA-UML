// Domain/Entities/BookCopy.cs  +  Infrastructure/Persistence/Configurations/BookCopyConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Domain.Entities;

public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class BookCopy : AuditableEntity
{
    public Guid BookId { get; private set; }
    public string Barcode { get; private set; } = string.Empty;
    public CopyStatus Status { get; private set; } = CopyStatus.Available;

    public bool CanBeBorrowed => Status == CopyStatus.Available;
}

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Barcode).IsUnique();   // CAT: mã vạch duy nhất
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);      // xóa mềm
    }
}
