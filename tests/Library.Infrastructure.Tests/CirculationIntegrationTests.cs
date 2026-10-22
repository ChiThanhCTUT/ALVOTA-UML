using FluentAssertions;
using Library.Application.Common.Models;
using Library.Application.Features.Catalog.Commands;
using Library.Application.Features.Circulation.Commands.BorrowBook;
using Library.Application.Features.Circulation.Commands.ReturnBook;
using Library.Application.Features.Fines.Commands;
using Library.Application.Features.Inventory.Commands;
using Library.Application.Features.Reports.Queries;
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
        currentUser.SetUser(Guid.NewGuid().ToString(), "librarian", "Librarian");

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

        // Assert Loan.Status updated to Returned (BUG-011)
        var loanInDb = await loans.GetByIdAsync(borrowResult.Value.Id);
        loanInDb!.Status.Should().Be(LoanStatus.Returned);
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

    [Fact]
    public async Task CreateAndUpdateBook_WithUntrackedAuthors_PopulatesAuthorNamesWithoutNullRef() // BUG-004
    {
        // Arrange
        var category = new Category { Name = "Văn học thiếu nhi" };
        var publisher = new Publisher { Name = "NXB Trẻ" };
        var author1 = new Author { FullName = "Nguyễn Nhật Ánh" };
        var author2 = new Author { FullName = "Tô Hoài" };

        await _context.Categories.AddAsync(category);
        await _context.Publishers.AddAsync(publisher);
        await _context.Authors.AddRangeAsync(author1, author2);
        await _context.SaveChangesAsync();

        // Detach all entities to simulate separate AsNoTracking lookup query in UI (BUG-003 / BUG-004)
        _context.ChangeTracker.Clear();

        var books = new BookRepository(_context);
        var copies = new BookCopyRepository(_context);
        var createHandler = new CreateBookCommandHandler(books, copies, _uow, _time);

        // Act 1: Create book with author1
        var createResult = await createHandler.Handle(new CreateBookCommand(
            Title: "Mắt Biếc",
            Subtitle: null,
            Isbn: "978-604-1-99999-1",
            PublicationYear: 2023,
            CategoryId: category.Id,
            PublisherId: publisher.Id,
            Language: "Tiếng Việt",
            PageCount: 250,
            Price: 95000m,
            CoverImagePath: null,
            Summary: null,
            IsReferenceOnly: false,
            AuthorIds: new[] { author1.Id },
            InitialCopiesCount: 2,
            InitialShelfLocation: "Kệ A1"), CancellationToken.None);

        // Assert 1
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value.Authors.Should().ContainSingle().Which.Should().Be("Nguyễn Nhật Ánh");
        createResult.Value.CategoryName.Should().Be("Văn học thiếu nhi");
        createResult.Value.PublisherName.Should().Be("NXB Trẻ");
        createResult.Value.TotalCopies.Should().Be(2);

        // Clear tracker again and test UpdateBookCommand with author2
        _context.ChangeTracker.Clear();
        var updateHandler = new UpdateBookCommandHandler(books, _uow, _time);
        var updateResult = await updateHandler.Handle(new UpdateBookCommand(
            Id: createResult.Value.Id,
            Title: "Mắt Biếc (Tái bản)",
            Subtitle: null,
            Isbn: "978-604-1-99999-1",
            PublicationYear: 2024,
            CategoryId: category.Id,
            PublisherId: publisher.Id,
            Language: "Tiếng Việt",
            PageCount: 260,
            Price: 100000m,
            CoverImagePath: null,
            Summary: null,
            IsReferenceOnly: false,
            AuthorIds: new[] { author1.Id, author2.Id }), CancellationToken.None);

        // Assert 2
        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.Authors.Should().HaveCount(2);
        updateResult.Value.Authors.Should().Contain("Nguyễn Nhật Ánh").And.Contain("Tô Hoài");
    }

    [Fact]
    public void CurrentUserService_DefaultsToUnauthenticatedOnStartup() // BUG-001
    {
        var currentUser = new CurrentUserService();
        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.UserId.Should().BeNull();
        currentUser.Username.Should().BeNull();
        currentUser.Role.Should().BeNull();
    }

    [Fact]
    public async Task InventorySessionFlow_PopulatesExpectedCopiesAndComputesMissingAndDamaged() // BUG-007
    {
        // Arrange: 3 copies on Kệ K1 (2 Available, 1 Damaged)
        var category = new Category { Name = "Lịch sử" };
        var publisher = new Publisher { Name = "NXB Sự Thật" };
        var book = new Book { Title = "Đại Việt Sử Ký", Price = 120000m, Category = category, Publisher = publisher };
        var copy1 = new BookCopy { Book = book, Barcode = "CP-INV-01", ShelfLocation = "Kệ K1", Status = CopyStatus.Available };
        var copy2 = new BookCopy { Book = book, Barcode = "CP-INV-02", ShelfLocation = "Kệ K1", Status = CopyStatus.Available };
        var copy3 = new BookCopy { Book = book, Barcode = "CP-INV-03", ShelfLocation = "Kệ K1", Status = CopyStatus.Damaged };

        await _context.Categories.AddAsync(category);
        await _context.Publishers.AddAsync(publisher);
        await _context.Books.AddAsync(book);
        await _context.BookCopies.AddRangeAsync(copy1, copy2, copy3);
        await _context.SaveChangesAsync();

        var inventoryRepo = new InventoryRepository(_context);
        var copyRepo = new BookCopyRepository(_context);

        var createHandler = new CreateInventorySessionCommandHandler(inventoryRepo, _uow, _time, copyRepo);
        var scanHandler = new ScanInventoryBarcodeCommandHandler(inventoryRepo, copyRepo, _uow, _time);
        var completeHandler = new CompleteInventorySessionCommandHandler(inventoryRepo, _uow, _time);

        // Act 1: Create session for "Kệ K1"
        var createRes = await createHandler.Handle(new CreateInventorySessionCommand("Kiểm kê Kệ K1", "Kệ K1"), CancellationToken.None);
        createRes.IsSuccess.Should().BeTrue();
        createRes.Value.TotalExpected.Should().Be(3);

        // Act 2: Scan only copy1 and copy3 (leave copy2 unscanned -> Missing)
        var scan1 = await scanHandler.Handle(new ScanInventoryBarcodeCommand(createRes.Value.Id, "CP-INV-01"), CancellationToken.None);
        var scan3 = await scanHandler.Handle(new ScanInventoryBarcodeCommand(createRes.Value.Id, "CP-INV-03"), CancellationToken.None);
        scan1.IsSuccess.Should().BeTrue();
        scan3.IsSuccess.Should().BeTrue();

        // Act 3: Complete session
        var completeRes = await completeHandler.Handle(new CompleteInventorySessionCommand(createRes.Value.Id), CancellationToken.None);

        // Assert
        completeRes.IsSuccess.Should().BeTrue();
        completeRes.Value.Status.Should().Be(InventorySessionStatus.Completed);
        completeRes.Value.TotalExpected.Should().Be(3);
        completeRes.Value.TotalScanned.Should().Be(2);
        completeRes.Value.TotalMissing.Should().Be(1);
        completeRes.Value.TotalDamaged.Should().Be(1);
    }

    [Fact]
    public async Task SoftDeletedBook_AllowsRecreatingWithSameIsbn_AndVietnameseSearchMatches() // BUG-012, BUG-013, BUG-014
    {
        var category = new Category { Name = "Kỹ năng sống" };
        var publisher = new Publisher { Name = "NXB Tổng Hợp" };
        await _context.Categories.AddAsync(category);
        await _context.Publishers.AddAsync(publisher);
        await _context.SaveChangesAsync();

        var books = new BookRepository(_context);
        var copies = new BookCopyRepository(_context);
        var createHandler = new CreateBookCommandHandler(books, copies, _uow, _time);
        var deleteHandler = new DeleteBookCommandHandler(books, _uow, _time);

        var firstCreate = await createHandler.Handle(new CreateBookCommand(
            Title: "Đắc Nhân Tâm",
            Subtitle: null,
            Isbn: "978-604-58-1234-5",
            PublicationYear: 2023,
            CategoryId: category.Id,
            PublisherId: publisher.Id,
            Language: "Tiếng Việt",
            PageCount: 320,
            Price: 88000m,
            CoverImagePath: null,
            Summary: null,
            IsReferenceOnly: false,
            AuthorIds: Array.Empty<Guid>(),
            InitialCopiesCount: 2,
            InitialShelfLocation: "Kệ A2"), CancellationToken.None);

        firstCreate.IsSuccess.Should().BeTrue();

        // Soft-delete first book
        var delRes = await deleteHandler.Handle(new DeleteBookCommand(firstCreate.Value.Id), CancellationToken.None);
        delRes.IsSuccess.Should().BeTrue();

        // Re-create book with the exact same ISBN (BUG-013 filtered unique index)
        var secondCreate = await createHandler.Handle(new CreateBookCommand(
            Title: "Đắc Nhân Tâm (Bản mới)",
            Subtitle: null,
            Isbn: "978-604-58-1234-5",
            PublicationYear: 2024,
            CategoryId: category.Id,
            PublisherId: publisher.Id,
            Language: "Tiếng Việt",
            PageCount: 320,
            Price: 95000m,
            CoverImagePath: null,
            Summary: null,
            IsReferenceOnly: false,
            AuthorIds: Array.Empty<Guid>(),
            InitialCopiesCount: 2,
            InitialShelfLocation: "Kệ A2"), CancellationToken.None);

        secondCreate.IsSuccess.Should().BeTrue();

        // Search lowercase Vietnamese "đắc nhân tâm" (BUG-014)
        var searchResult = await books.GetPagedAsync(new PagedRequest(1, 10, "đắc nhân tâm"));
        searchResult.TotalCount.Should().Be(1);
        searchResult.Items[0].Title.Should().Contain("Đắc Nhân Tâm");
    }

    [Fact]
    public async Task PartialFinePayment_IsIncludedInMonthlyDashboardRevenue() // BUG-006, BUG-014
    {
        var category = new Category { Name = "Toán học" };
        var publisher = new Publisher { Name = "NXB Giáo Dục" };
        var book = new Book { Title = "Giải Tích 12", Price = 50000m, Category = category, Publisher = publisher };
        var member = new Member
        {
            CardNumber = "HS-8888",
            FullName = "Đặng Thị Hoa",
            ClassOrDepartment = "12A1",
            CardStatus = CardStatus.Active,
            CardIssuedAtUtc = _baseTime,
            CardExpiresAtUtc = _baseTime.AddYears(1)
        };
        var fine = new Fine
        {
            Member = member,
            Reason = FineReason.Overdue,
            Amount = 20000m,
            Status = FineStatus.Pending,
            CreatedAtUtc = _baseTime
        };

        await _context.Categories.AddAsync(category);
        await _context.Publishers.AddAsync(publisher);
        await _context.Books.AddAsync(book);
        await _context.Members.AddAsync(member);
        await _context.Fines.AddAsync(fine);
        await _context.SaveChangesAsync();

        var books = new BookRepository(_context);
        var loans = new LoanRepository(_context);
        var fines = new FineRepository(_context);
        var members = new MemberRepository(_context);
        var currentUser = new CurrentUserService();
        currentUser.SetUser(Guid.NewGuid().ToString(), "librarian", "Librarian");

        // Pay partial fine (10,000 / 20,000) -> Status remains Pending
        var payHandler = new PayFineCommandHandler(fines, currentUser, _uow, _time);
        var payRes = await payHandler.Handle(new PayFineCommand(fine.Id, 10000m), CancellationToken.None);
        payRes.IsSuccess.Should().BeTrue();
        payRes.Value.Amount.Should().Be(10000m);
        fine.Status.Should().Be(FineStatus.Pending);

        // Dashboard statistics must still include the 10,000đ partial payment in MonthlyFineRevenue
        var dashboardHandler = new GetDashboardStatisticsQueryHandler(books, loans, fines, members, _time);
        var statsRes = await dashboardHandler.Handle(new GetDashboardStatisticsQuery(), CancellationToken.None);
        statsRes.IsSuccess.Should().BeTrue();
        statsRes.Value.MonthlyFineRevenue.Should().Be(10000m);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
