// tests/Library.Application.Tests/Circulation/BorrowBookCommandHandlerTests.cs
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Library.Application.Tests.Circulation;

public class BorrowBookCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_MemberHasOverdueLoan_ReturnsHasOverdueError() // CIR-02
    {
        // Arrange
        var time = new FakeTimeProvider(Now);
        var sut = HandlerBuilder.Create(time).WithMemberHavingOverdueLoan().Build();

        // Act
        var result = await sut.Handle(new BorrowBookCommand(MemberIds.Overdue, "CP-0001"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CIR.HAS_OVERDUE");
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesLoanWithDueDate() // CIR-01, BR-03
    {
        var time = new FakeTimeProvider(Now);
        var sut = HandlerBuilder.Create(time).WithStudentMember().WithAvailableCopy("CP-0001").Build();

        var result = await sut.Handle(new BorrowBookCommand(MemberIds.Student, "CP-0001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DueDate.Should().Be(Now.AddDays(14));
    }
}
