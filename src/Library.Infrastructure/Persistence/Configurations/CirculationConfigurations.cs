using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.CardNumber).IsUnique().HasFilter("[IsDeleted] = 0"); // MEM: Mã thẻ duy nhất (loại trừ đã xóa mềm)
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ClassOrDepartment).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Member)
            .WithMany(m => m.Loans)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoanItemConfiguration : IEntityTypeConfiguration<LoanItem>
{
    public void Configure(EntityTypeBuilder<LoanItem> builder)
    {
        builder.ToTable("LoanItems");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.BookCopyId).IsUnique().HasFilter("[ReturnedAtUtc] IS NULL AND [IsDeleted] = 0");
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Loan)
            .WithMany(l => l.Items)
            .HasForeignKey(x => x.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BookCopy)
            .WithMany(c => c.LoanItems)
            .HasForeignKey(x => x.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Fine)
            .WithOne(f => f.LoanItem)
            .HasForeignKey<Fine>(f => f.LoanItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Book)
            .WithMany(b => b.Reservations)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Member)
            .WithMany(m => m.Reservations)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReservedCopy)
            .WithMany()
            .HasForeignKey(x => x.ReservedCopyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("Fines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Member)
            .WithMany(m => m.Fines)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ReceivedBy).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Fine)
            .WithMany(f => f.Payments)
            .HasForeignKey(x => x.FineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
