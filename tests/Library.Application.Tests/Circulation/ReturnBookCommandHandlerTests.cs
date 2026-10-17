using FluentAssertions;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.Commands.ReturnBook;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Library.Application.Tests.Circulation;

public class ReturnBookCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ReturnOverdueBook_CreatesFineAndCalculatesLateFine() // CIR-03, BR-05, BR-06
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var barcode = "CP-99999";
        var book = new Book { Title = "Lập trình C#", Price = 100000m };
        var copy = new BookCopy { Barcode = barcode, Book = book, Status = CopyStatus.Borrowed };

        var loan = new Loan
        {
            MemberId = Guid.NewGuid(),
            Member = new Member { FullName = "Nguyễn Văn A" }
        };

        var loanItem = new LoanItem
        {
            LoanId = loan.Id,
            Loan = loan,
            BookCopyId = copy.Id,
            BookCopy = copy,
            DueDateUtc = FixedNow.AddDays(-5), // 5 days late -> 5 * 1000 = 5,000 VND
            ReturnedAtUtc = null
        };

        var mockLoans = new Mock<ILoanRepository>();
        mockLoans.Setup(l => l.GetActiveLoanItemByCopyBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loanItem);

        var mockCopies = new Mock<IBookCopyRepository>();
        var mockFines = new Mock<IFineRepository>();
        var mockReservations = new Mock<IReservationRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new ReturnBookCommandHandler(
            mockLoans.Object,
            mockCopies.Object,
            mockFines.Object,
            mockReservations.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new ReturnBookCommand(barcode, ReturnCondition.Normal), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverdueDays.Should().Be(5);
        result.Value.FineAmount.Should().Be(5000m);
        loanItem.IsReturned.Should().BeTrue();
        copy.Status.Should().Be(CopyStatus.Available);
        mockFines.Verify(f => f.AddAsync(It.IsAny<Fine>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnDamagedBook_CreatesDamagedFine() // CIR-04, BR-08
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var barcode = "CP-88888";
        var book = new Book { Title = "Giải tích", Price = 100000m };
        var copy = new BookCopy { Barcode = barcode, Book = book, Status = CopyStatus.Borrowed };

        var loan = new Loan
        {
            MemberId = Guid.NewGuid(),
            Member = new Member { FullName = "Trần Thị B" }
        };

        var loanItem = new LoanItem
        {
            LoanId = loan.Id,
            Loan = loan,
            BookCopyId = copy.Id,
            BookCopy = copy,
            DueDateUtc = FixedNow.AddDays(2), // not overdue
            ReturnedAtUtc = null
        };

        var mockLoans = new Mock<ILoanRepository>();
        mockLoans.Setup(l => l.GetActiveLoanItemByCopyBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loanItem);

        var mockCopies = new Mock<IBookCopyRepository>();
        var mockFines = new Mock<IFineRepository>();
        var mockReservations = new Mock<IReservationRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new ReturnBookCommandHandler(
            mockLoans.Object,
            mockCopies.Object,
            mockFines.Object,
            mockReservations.Object,
            mockUow.Object,
            time);

        // Act: return with heavy damage (50% of 100,000 = 50,000 VND)
        var result = await handler.Handle(new ReturnBookCommand(barcode, ReturnCondition.DamagedHeavy), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FineAmount.Should().Be(50000m);
        copy.Status.Should().Be(CopyStatus.Damaged);
    }
}
