using FluentAssertions;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Services;
using Xunit;

namespace Library.Domain.Tests;

public class BusinessRulesTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetBorrowLimit_StudentMember_ReturnsThreeCopies() // BR-01
    {
        // Arrange
        var member = new Member { Type = MemberType.Student };

        // Act
        var limit = member.GetBorrowLimit();

        // Assert
        limit.Should().Be(3);
    }

    [Fact]
    public void GetBorrowLimit_TeacherMember_ReturnsTenCopies() // BR-02
    {
        // Arrange
        var member = new Member { Type = MemberType.Teacher };

        // Act
        var limit = member.GetBorrowLimit();

        // Assert
        limit.Should().Be(10);
    }

    [Fact]
    public void GetLoanDurationDays_StudentAndTeacher_ReturnsFourteenAndThirtyDays() // BR-03
    {
        // Arrange
        var student = new Member { Type = MemberType.Student };
        var teacher = new Member { Type = MemberType.Teacher };

        // Act & Assert
        student.GetLoanDurationDays().Should().Be(14);
        teacher.GetLoanDurationDays().Should().Be(30);
    }

    [Fact]
    public void CanRenew_RenewalCountAtMax_ReturnsRenewNotAllowedError() // BR-04
    {
        // Arrange
        var item = new LoanItem
        {
            DueDateUtc = FixedNow.AddDays(2),
            RenewalCount = 2,
            ReturnedAtUtc = null
        };

        // Act
        var result = CirculationPolicyService.CanRenew(item, FixedNow, hasActiveReservation: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.RENEW_NOT_ALLOWED");
    }

    [Fact]
    public void CanRenew_LoanIsOverdue_ReturnsRenewNotAllowedError() // BR-04, CIR-05
    {
        // Arrange
        var item = new LoanItem
        {
            DueDateUtc = FixedNow.AddDays(-1), // overdue
            RenewalCount = 0,
            ReturnedAtUtc = null
        };

        // Act
        var result = CirculationPolicyService.CanRenew(item, FixedNow, hasActiveReservation: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.RENEW_NOT_ALLOWED");
    }

    [Fact]
    public void CanRenew_HasActiveReservation_ReturnsRenewNotAllowedError() // BR-04, CIR-05
    {
        // Arrange
        var item = new LoanItem
        {
            DueDateUtc = FixedNow.AddDays(3),
            RenewalCount = 0,
            ReturnedAtUtc = null
        };

        // Act
        var result = CirculationPolicyService.CanRenew(item, FixedNow, hasActiveReservation: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.RENEW_NOT_ALLOWED");
    }

    [Fact]
    public void Renew_ValidItem_IncrementsCountAndExtendsDueDateBySevenDays() // BR-04
    {
        // Arrange
        var initialDueDate = FixedNow.AddDays(2);
        var item = new LoanItem
        {
            DueDateUtc = initialDueDate,
            RenewalCount = 0,
            ReturnedAtUtc = null
        };

        // Act
        item.Renew(FixedNow);

        // Assert
        item.RenewalCount.Should().Be(1);
        item.DueDateUtc.Should().Be(initialDueDate.AddDays(7));
    }

    [Fact]
    public void CalculateOverdueFine_ThreeDaysOverdue_CalculatesCorrectAmount() // BR-05
    {
        // Arrange
        var dueDate = FixedNow.AddDays(-3);
        var returnDate = FixedNow;
        var coverPrice = 100000m;

        // Act
        var fine = FineCalculator.CalculateOverdueFine(dueDate, returnDate, coverPrice, dailyRate: 1000m);

        // Assert
        fine.Should().Be(3000m);
    }

    [Fact]
    public void CalculateOverdueFine_ExceedsCoverPrice_CapsAtCoverPrice() // BR-06
    {
        // Arrange
        var dueDate = FixedNow.AddDays(-60); // 60 days * 1000 = 60,000 VND
        var returnDate = FixedNow;
        var coverPrice = 50000m;

        // Act
        var fine = FineCalculator.CalculateOverdueFine(dueDate, returnDate, coverPrice, dailyRate: 1000m);

        // Assert
        fine.Should().Be(50000m);
    }

    [Fact]
    public void CalculateLostFine_WithCoverPriceAndProcessingFee_ReturnsTotal() // BR-07
    {
        // Arrange
        var coverPrice = 80000m;

        // Act
        var fine = FineCalculator.CalculateLostFine(coverPrice, processingFee: 20000m);

        // Assert
        fine.Should().Be(100000m);
    }

    [Fact]
    public void CalculateDamagedFine_LightAndHeavyDamage_ReturnsTwentyAndFiftyPercent() // BR-08
    {
        // Arrange
        var coverPrice = 100000m;

        // Act
        var lightFine = FineCalculator.CalculateDamagedFine(coverPrice, ReturnCondition.DamagedLight);
        var heavyFine = FineCalculator.CalculateDamagedFine(coverPrice, ReturnCondition.DamagedHeavy);

        // Assert
        lightFine.Should().Be(20000m);
        heavyFine.Should().Be(50000m);
    }

    [Fact]
    public void CanBorrow_UnpaidDebtExceedsThreshold_ReturnsDebtExceededError() // BR-09
    {
        // Arrange
        var member = new Member
        {
            CardStatus = CardStatus.Active,
            CardExpiresAtUtc = FixedNow.AddMonths(6)
        };
        var copy = new BookCopy { Status = CopyStatus.Available, Book = new Book() };

        // Act: debt = 25,000 > 20,000 threshold
        var result = CirculationPolicyService.CanBorrow(
            member, copy, activeLoansCount: 0, hasOverdueLoans: false, unpaidFineAmount: 25000m, FixedNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FIN.DEBT_EXCEEDED");
    }

    [Fact]
    public void NotifyAvailable_ReservationHoldsCopyForThreeDays() // BR-10
    {
        // Arrange
        var reservation = new Reservation();
        var copy = new BookCopy();

        // Act
        reservation.NotifyAvailable(copy, FixedNow, holdDays: 3);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Notified);
        reservation.ReservedCopyId.Should().Be(copy.Id);
        reservation.HoldExpiresAtUtc.Should().Be(FixedNow.AddDays(3));
    }

    [Fact]
    public void CanBorrow_BookIsReferenceOnly_ReturnsCannotBorrowReferenceError() // BR-11
    {
        // Arrange
        var member = new Member
        {
            CardStatus = CardStatus.Active,
            CardExpiresAtUtc = FixedNow.AddMonths(6)
        };
        var book = new Book { IsReferenceOnly = true };
        var copy = new BookCopy { Status = CopyStatus.Available, Book = book };

        // Act
        var result = CirculationPolicyService.CanBorrow(
            member, copy, activeLoansCount: 0, hasOverdueLoans: false, unpaidFineAmount: 0m, FixedNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.CANNOT_BORROW_REFERENCE");
    }

    [Fact]
    public void MarkDeleted_AuditableEntity_SetsIsDeletedAndAuditFields() // BR-12
    {
        // Arrange
        var book = new Book();

        // Act
        book.MarkDeleted("admin", FixedNow);

        // Assert
        book.IsDeleted.Should().BeTrue();
        book.UpdatedBy.Should().Be("admin");
        book.UpdatedAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public void AddPayment_PartialPaymentThenRemainingPayment_UpdatesStatusAndUnpaidTotalCorrectly() // BUG-006
    {
        // Arrange
        var fine = new Fine { Amount = 10000m, Status = FineStatus.Pending };
        var member = new Member();
        member.Fines.Add(fine);

        // Act 1: Pay 5,000 VND (50%)
        fine.AddPayment(5000m, "librarian", "BL-001", FixedNow);

        // Assert 1: Still Pending, RemainingAmount = 5,000 VND
        fine.PaidAmount.Should().Be(5000m);
        fine.RemainingAmount.Should().Be(5000m);
        fine.Status.Should().Be(FineStatus.Pending);
        member.GetUnpaidFineTotal().Should().Be(5000m);

        // Act 2: Pay remaining 5,000 VND
        fine.AddPayment(5000m, "librarian", "BL-002", FixedNow);

        // Assert 2: Now Paid, RemainingAmount = 0 VND
        fine.PaidAmount.Should().Be(10000m);
        fine.RemainingAmount.Should().Be(0m);
        fine.Status.Should().Be(FineStatus.Paid);
        member.GetUnpaidFineTotal().Should().Be(0m);
    }
}
