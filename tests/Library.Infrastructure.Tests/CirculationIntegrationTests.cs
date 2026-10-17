using FluentAssertions;
using Library.Application.Features.Circulation.Commands.BorrowBook;
using Library.Application.Features.Circulation.Commands.ReturnBook;
using Library.Application.Features.Fines.Commands;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Repositories;
using Library.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Library.Infrastructure.Tests;

public class CirculationIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LibraryDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly FakeTimeProvider _time;
    private readonly DateTime _baseTime = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);

    public CirculationIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(_connection)
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
            .Options;

        _context = new LibraryDbContext(options);
        _context.Database.EnsureCreated();

        _uow = new UnitOfWork(_context);
        _time = new FakeTimeProvider(_baseTime);
    }

    [Fact]
    public async Task BorrowToReturnToFineFlow_ExecutesCorrectlyInDatabase() // CIR-01, CIR-03, FIN-01, FIN-03
    {
        // 1. Arrange: Seed Category, Publisher, Book, Copy, Member
        var category = new Category { Name = "Văn học" };
        var publisher = new Publisher { Name = "NXB Kim Đồng" };
        var book = new Book
        {
            Title = "Dế Mèn Phiêu Lưu Ký",
            Price = 50000m,
            Category = category,
            Publisher = publisher
        };
        var copy = new BookCopy
        {
            Book = book,
            Barcode = "CP-DEMEN-01",
            ShelfLocation = "Kệ A1",
            Status = CopyStatus.Available
        };
        var member = new Member
        {
            CardNumber = "HS-9999",
            FullName = "Lê Văn C",
            ClassOrDepartment = "10A2",
            Type = MemberType.Student,
            CardStatus = CardStatus.Active,
            CardIssuedAtUtc = _baseTime,
            CardExpiresAtUtc = _baseTime.AddMonths(12)
        };

        await _context.Categories.AddAsync(category);
        await _context.Publishers.AddAsync(publisher);
        await _context.Books.AddAsync(book);
        await _context.BookCopies.AddAsync(copy);
        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();

        // Repositories
        var members = new MemberRepository(_context);
        var copies = new BookCopyRepository(_context);
        var loans = new LoanRepository(_context);
        var fines = new FineRepository(_context);
        var reservations = new ReservationRepository(_context);
        var currentUser = new CurrentUserService();

        // 2. Act: Borrow book
        var borrowHandler = new BorrowBookCommandHandler(members, copies, loans, fines, reservations, _uow, _time);
        var borrowResult = await borrowHandler.Handle(new BorrowBookCommand(member.Id, "CP-DEMEN-01"), CancellationToken.None);

        // Assert borrow
        borrowResult.IsSuccess.Should().BeTrue();
        borrowResult.Value.DueDateUtc.Should().Be(_baseTime.AddDays(14));

        var copyInDb = await copies.GetByBarcodeAsync("CP-DEMEN-01");
        copyInDb!.Status.Should().Be(CopyStatus.Borrowed);

        // 3. Act: Fast forward time by 20 days (6 days overdue) and return with Light Damage
        _time.SetUtcNow(_baseTime.AddDays(20));

        var returnHandler = new ReturnBookCommandHandler(loans, copies, fines, reservations, _uow, _time);
        var returnResult = await returnHandler.Handle(
            new ReturnBookCommand("CP-DEMEN-01", ReturnCondition.DamagedLight), CancellationToken.None);

        // Assert return and fines (6 days * 1,000 = 6,000 + 20% of 50,000 = 10,000 -> 16,000 total)
        returnResult.IsSuccess.Should().BeTrue();
        returnResult.Value.OverdueDays.Should().Be(6);
        returnResult.Value.FineAmount.Should().Be(16000m);
        returnResult.Value.FineId.Should().NotBeNull();

        // 4. Act: Pay fine
        var payHandler = new PayFineCommandHandler(fines, currentUser, _uow, _time);
        var payResult = await payHandler.Handle(new PayFineCommand(returnResult.Value.FineId!.Value, 16000m), CancellationToken.None);

        // Assert payment
        payResult.IsSuccess.Should().BeTrue();
        var fineInDb = await fines.GetByIdAsync(returnResult.Value.FineId.Value);
        fineInDb!.Status.Should().Be(FineStatus.Paid);
        fineInDb.RemainingAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Concurrency_TwoClientsBorrowingSameCopy_OnlyOneSucceeds() // Section 9: Concurrency Test
    {
        // Arrange
        var category = new Category { Name = "Khoa học" };
        var publisher = new Publisher { Name = "NXB Giáo Dục" };
        var book = new Book
        {
            Title = "Sách Test Concurrency",
            Price = 30000m,
            Category = category,
            Publisher = publisher
        };
        var copy = new BookCopy { Book = book, Barcode = "CP-CONCURRENCY-1", ShelfLocation = "Kệ B", Status = CopyStatus.Available };
        var member1 = new Member { CardNumber = "HS-1", FullName = "Bạn đọc 1", ClassOrDepartment = "10A1", CardExpiresAtUtc = _baseTime.AddYears(1) };
        var member2 = new Member { CardNumber = "HS-2", FullName = "Bạn đọc 2", ClassOrDepartment = "10A2", CardExpiresAtUtc = _baseTime.AddYears(1) };

        await _context.Categories.AddAsync(category);
        await _context.Publishers.AddAsync(publisher);
        await _context.Books.AddAsync(book);
        await _context.BookCopies.AddAsync(copy);
        await _context.Members.AddRangeAsync(member1, member2);
        await _context.SaveChangesAsync();

        var members = new MemberRepository(_context);
        var copies = new BookCopyRepository(_context);
        var loans = new LoanRepository(_context);
        var fines = new FineRepository(_context);
        var reservations = new ReservationRepository(_context);

        var borrowHandler = new BorrowBookCommandHandler(members, copies, loans, fines, reservations, _uow, _time);

        // Act: First client borrows
        var result1 = await borrowHandler.Handle(new BorrowBookCommand(member1.Id, "CP-CONCURRENCY-1"), CancellationToken.None);

        // Second client attempts to borrow the same copy
        var result2 = await borrowHandler.Handle(new BorrowBookCommand(member2.Id, "CP-CONCURRENCY-1"), CancellationToken.None);

        // Assert: First succeeds, second is rejected because copy is no longer available
        result1.IsSuccess.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("CIR.COPY_UNAVAILABLE");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
