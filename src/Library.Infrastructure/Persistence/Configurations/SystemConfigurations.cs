using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class InventorySessionConfiguration : IEntityTypeConfiguration<InventorySession>
{
    public void Configure(EntityTypeBuilder<InventorySession> builder)
    {
        builder.ToTable("InventorySessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScannedBarcode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.InventorySession)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.InventorySessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BookCopy)
            .WithMany(c => c.InventoryItems)
            .HasForeignKey(x => x.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Username).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.Salt).IsRequired();
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
