using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _service = new UserService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnListOfUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User("User 1", "user1@test.com", "hash", UserRole.User),
            new User("User 2", "user2@test.com", "hash", UserRole.Admin)
        };
        _unitOfWorkMock.Setup(x => x.Users.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act
        var result = await _service.GetAllUsersAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Any(u => u.Email == "user1@test.com").Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("Test User", "test@test.com", "hash", UserRole.User) { Id = userId };
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ShouldUpdateRole_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("Test User", "test@test.com", "hash", UserRole.User) { Id = userId };
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _service.UpdateUserRoleAsync(userId, UserRole.Admin);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Role.Should().Be(UserRole.Admin.ToString());
        user.Role.Should().Be(UserRole.Admin);
        _unitOfWorkMock.Verify(x => x.Users.Update(user), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldDelete_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("Test User", "test@test.com", "hash", UserRole.User) { Id = userId };
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _service.DeleteUserAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.Users.Delete(user), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
