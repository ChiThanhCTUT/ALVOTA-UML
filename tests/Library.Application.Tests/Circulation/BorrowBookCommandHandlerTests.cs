using FluentAssertions;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.Commands.BorrowBook;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Library.Application.Tests.Circulation;

public class BorrowBookCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_MemberHasOverdueLoan_ReturnsHasOverdueError() // CIR-02
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var member = new Member
        {
            CardStatus = CardStatus.Active,
            CardExpiresAtUtc = FixedNow.AddMonths(6),
            Type = MemberType.Student
        };
        var memberId = member.Id;
        var copy = new BookCopy { Status = CopyStatus.Available, Book = new Book() };

        var mockMembers = new Mock<IMemberRepository>();
        mockMembers.Setup(m => m.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var mockCopies = new Mock<IBookCopyRepository>();
        mockCopies.Setup(c => c.GetByBarcodeAsync("CP-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(copy);

        var mockLoans = new Mock<ILoanRepository>();
        // Member has an overdue loan item
        var overdueItem = new LoanItem
        {
            DueDateUtc = FixedNow.AddDays(-2),
            ReturnedAtUtc = null
        };
        mockLoans.Setup(l => l.GetActiveLoanItemsByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanItem> { overdueItem });

        var mockFines = new Mock<IFineRepository>();
        mockFines.Setup(f => f.GetUnpaidByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fine>());

        var mockReservations = new Mock<IReservationRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new BorrowBookCommandHandler(
            mockMembers.Object,
            mockCopies.Object,
            mockLoans.Object,
            mockFines.Object,
            mockReservations.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new BorrowBookCommand(memberId, "CP-001"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.HAS_OVERDUE");
    }

    [Fact]
    public async Task Handle_StudentBorrowLimitReached_ReturnsLimitReachedError() // CIR-02, BR-01
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var member = new Member
        {
            CardStatus = CardStatus.Active,
            CardExpiresAtUtc = FixedNow.AddMonths(6),
            Type = MemberType.Student // Limit 3
        };
        var memberId = member.Id;
        var copy = new BookCopy { Status = CopyStatus.Available, Book = new Book() };

        var mockMembers = new Mock<IMemberRepository>();
        mockMembers.Setup(m => m.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var mockCopies = new Mock<IBookCopyRepository>();
        mockCopies.Setup(c => c.GetByBarcodeAsync("CP-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(copy);

        var mockLoans = new Mock<ILoanRepository>();
        // Already borrowing 3 books
        var activeLoans = new List<LoanItem>
        {
            new() { DueDateUtc = FixedNow.AddDays(5) },
            new() { DueDateUtc = FixedNow.AddDays(5) },
            new() { DueDateUtc = FixedNow.AddDays(5) }
        };
        mockLoans.Setup(l => l.GetActiveLoanItemsByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeLoans);

        var mockFines = new Mock<IFineRepository>();
        mockFines.Setup(f => f.GetUnpaidByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fine>());

        var mockReservations = new Mock<IReservationRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new BorrowBookCommandHandler(
            mockMembers.Object,
            mockCopies.Object,
            mockLoans.Object,
            mockFines.Object,
            mockReservations.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new BorrowBookCommand(memberId, "CP-001"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.LIMIT_REACHED");
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesLoanWithFourteenDaysDueDate() // CIR-01, BR-03
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var member = new Member
        {
            CardStatus = CardStatus.Active,
            CardExpiresAtUtc = FixedNow.AddMonths(6),
            Type = MemberType.Student
        };
        var memberId = member.Id;
        var copy = new BookCopy { Status = CopyStatus.Available, Book = new Book() };

        var mockMembers = new Mock<IMemberRepository>();
        mockMembers.Setup(m => m.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var mockCopies = new Mock<IBookCopyRepository>();
        mockCopies.Setup(c => c.GetByBarcodeAsync("CP-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(copy);

        var mockLoans = new Mock<ILoanRepository>();
        mockLoans.Setup(l => l.GetActiveLoanItemsByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanItem>());

        var mockFines = new Mock<IFineRepository>();
        mockFines.Setup(f => f.GetUnpaidByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fine>());

        var mockReservations = new Mock<IReservationRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new BorrowBookCommandHandler(
            mockMembers.Object,
            mockCopies.Object,
            mockLoans.Object,
            mockFines.Object,
            mockReservations.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new BorrowBookCommand(memberId, "CP-001"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DueDateUtc.Should().Be(FixedNow.AddDays(14));
        copy.Status.Should().Be(CopyStatus.Borrowed);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotifiedMemberBorrowsReservedCopy_FulfillsReservationAndCreatesLoan() // BUG-005
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var member = new Member
        {
            CardStatus = CardStatus.Active,
            CardExpiresAtUtc = FixedNow.AddMonths(6),
            Type = MemberType.Student
        };
        var book = new Book { Title = "Cấu trúc dữ liệu" };
        var copy = new BookCopy
        {
            BookId = book.Id,
            Book = book,
            Barcode = "CP-RES-01",
            Status = CopyStatus.Reserved
        };
        var reservation = new Reservation
        {
            BookId = book.Id,
            MemberId = member.Id,
            ReservedCopyId = copy.Id,
            Status = ReservationStatus.Notified,
            NotifiedAtUtc = FixedNow.AddDays(-1),
            HoldExpiresAtUtc = FixedNow.AddDays(2)
        };

        var mockMembers = new Mock<IMemberRepository>();
        mockMembers.Setup(m => m.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var mockCopies = new Mock<IBookCopyRepository>();
        mockCopies.Setup(c => c.GetByBarcodeAsync("CP-RES-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(copy);

        var mockLoans = new Mock<ILoanRepository>();
        mockLoans.Setup(l => l.GetActiveLoanItemsByMemberIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanItem>());

        var mockFines = new Mock<IFineRepository>();
        mockFines.Setup(f => f.GetUnpaidByMemberIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Fine>());

        var mockReservations = new Mock<IReservationRepository>();
        mockReservations.Setup(r => r.GetActiveByMemberIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation> { reservation });

        var mockUow = new Mock<IUnitOfWork>();

        var handler = new BorrowBookCommandHandler(
            mockMembers.Object,
            mockCopies.Object,
            mockLoans.Object,
            mockFines.Object,
            mockReservations.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new BorrowBookCommand(member.Id, "CP-RES-01"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Completed);
        copy.Status.Should().Be(CopyStatus.Borrowed);
    }
}
