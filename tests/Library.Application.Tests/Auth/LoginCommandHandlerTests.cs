using FluentAssertions;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Auth.Commands;
using Library.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Library.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_FiveConsecutiveFailedLogins_LocksAccountForFiveMinutes() // AUTH-03
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var user = new User
        {
            Username = "librarian1",
            PasswordHash = "hashed_pw",
            Salt = "salt",
            FailedLoginAttempts = 4, // 5th attempt will lock
            Role = new Role { Name = "Librarian" }
        };

        var mockUsers = new Mock<IUserRepository>();
        mockUsers.Setup(u => u.GetByUsernameAsync("librarian1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var mockHasher = new Mock<IPasswordHasher>();
        mockHasher.Setup(h => h.VerifyPassword("wrong_pw", "hashed_pw", "salt"))
            .Returns(false);

        var mockUow = new Mock<IUnitOfWork>();

        var handler = new LoginCommandHandler(
            mockUsers.Object,
            mockHasher.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new LoginCommand("librarian1", "wrong_pw"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH.ACCOUNT_LOCKED");
        user.FailedLoginAttempts.Should().Be(5);
        user.LockoutEndUtc.Should().Be(FixedNow.AddMinutes(5));
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsAccountDisabledError() // BUG-002
    {
        // Arrange
        var time = new FakeTimeProvider(FixedNow);
        var user = new User
        {
            Username = "disabled_user",
            PasswordHash = "hashed_pw",
            Salt = "salt",
            IsActive = false,
            Role = new Role { Name = "Librarian" }
        };

        var mockUsers = new Mock<IUserRepository>();
        mockUsers.Setup(u => u.GetByUsernameAsync("disabled_user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var mockHasher = new Mock<IPasswordHasher>();
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new LoginCommandHandler(
            mockUsers.Object,
            mockHasher.Object,
            mockUow.Object,
            time);

        // Act
        var result = await handler.Handle(new LoginCommand("disabled_user", "any_pw"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH.ACCOUNT_DISABLED");
    }
}
