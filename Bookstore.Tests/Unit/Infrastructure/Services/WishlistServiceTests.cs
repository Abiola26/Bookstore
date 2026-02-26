using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Services;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for WishlistService
/// Tests all wishlist operations: add, remove, get, check
/// </summary>
public class WishlistServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IWishlistRepository> _wishlistRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<ILogger<WishlistService>> _loggerMock;
    private readonly WishlistService _service;

    public WishlistServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _wishlistRepositoryMock = new Mock<IWishlistRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _loggerMock = new Mock<ILogger<WishlistService>>();

        _unitOfWorkMock.Setup(u => u.Wishlist).Returns(_wishlistRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Books).Returns(_bookRepositoryMock.Object);

        _service = new WishlistService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // AddToWishlistAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddToWishlistAsync_WhenBookNotInWishlist_ShouldAddSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var book = new BookBuilder().Build();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ReturnsAsync(false);

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, ct))
            .ReturnsAsync(book);

        _wishlistRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WishlistItem>(), ct))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1);

        // Act
        var result = await _service.AddToWishlistAsync(userId, bookId, ct);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _wishlistRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), ct), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(ct), Times.Once);
    }

    [Fact]
    public async Task AddToWishlistAsync_WhenAlreadyInWishlist_ShouldReturn409()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddToWishlistAsync(userId, bookId, ct);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Contain("already in wishlist");
        _wishlistRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), ct), Times.Never);
    }

    [Fact]
    public async Task AddToWishlistAsync_WithNonexistentBook_ShouldReturn404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ReturnsAsync(false);

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, ct))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _service.AddToWishlistAsync(userId, bookId, ct);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Book not found");
    }

    [Fact]
    public async Task AddToWishlistAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.AddToWishlistAsync(userId, bookId, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }

    // ──────────────────────────────────────────────────────────────
    // RemoveFromWishlistAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromWishlistAsync_WhenItemExists_ShouldRemoveSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var wishlistItem = new WishlistItem(userId, bookId);

        _wishlistRepositoryMock
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, ct))
            .ReturnsAsync(wishlistItem);

        _wishlistRepositoryMock
            .Setup(r => r.Delete(wishlistItem));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RemoveFromWishlistAsync(userId, bookId, ct);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _wishlistRepositoryMock.Verify(r => r.Delete(wishlistItem), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(ct), Times.Once);
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_WhenItemNotInWishlist_ShouldReturn404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, ct))
            .ReturnsAsync((WishlistItem?)null);

        // Act
        var result = await _service.RemoveFromWishlistAsync(userId, bookId, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("not in wishlist");
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, ct))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.RemoveFromWishlistAsync(userId, bookId, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }

    // ──────────────────────────────────────────────────────────────
    // GetUserWishlistAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserWishlistAsync_ShouldReturnListOfBooks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var book1 = new BookBuilder().WithTitle("Book A").Build();
        var book2 = new BookBuilder().WithTitle("Book B").Build();

        var wishlistItems = new List<WishlistItem>
        {
            new WishlistItem(userId, book1.Id) { Book = book1 },
            new WishlistItem(userId, book2.Id) { Book = book2 }
        };

        _wishlistRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, ct))
            .ReturnsAsync(wishlistItems);

        // Act
        var result = await _service.GetUserWishlistAsync(userId, ct);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.Select(b => b.Title).Should().Contain("Book A").And.Contain("Book B");
    }

    [Fact]
    public async Task GetUserWishlistAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, ct))
            .ReturnsAsync(new List<WishlistItem>());

        // Act
        var result = await _service.GetUserWishlistAsync(userId, ct);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserWishlistAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, ct))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.GetUserWishlistAsync(userId, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }

    // ──────────────────────────────────────────────────────────────
    // IsInWishlistAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsInWishlistAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsInWishlistAsync(userId, bookId, ct);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task IsInWishlistAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IsInWishlistAsync(userId, bookId, ct);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task IsInWishlistAsync_WhenRepositoryThrows_ShouldReturn500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var ct = CancellationToken.None;

        _wishlistRepositoryMock
            .Setup(r => r.ExistsAsync(userId, bookId, ct))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.IsInWishlistAsync(userId, bookId, ct);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
    }
}
