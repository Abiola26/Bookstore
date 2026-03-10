using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class WishlistServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<WishlistService>> _loggerMock;
    private readonly WishlistService _service;

    public WishlistServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<WishlistService>>();
        _service = new WishlistService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldAdd_WhenBookExistsAndNotInWishlist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var book = new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid()) { Id = bookId };

        _uowMock.Setup(x => x.Wishlist.ExistsAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _service.AddToWishlistAsync(userId, bookId);

        // Assert
        result.Success.Should().BeTrue();
        _uowMock.Verify(x => x.Wishlist.AddAsync(It.IsAny<WishlistItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddToWishlistAsync_ShouldReturnConflict_WhenAlreadyInWishlist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        _uowMock.Setup(x => x.Wishlist.ExistsAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddToWishlistAsync(userId, bookId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be("Book is already in wishlist");
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_ShouldRemove_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var item = new WishlistItem(userId, bookId);

        _uowMock.Setup(x => x.Wishlist.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        var result = await _service.RemoveFromWishlistAsync(userId, bookId);

        // Assert
        result.Success.Should().BeTrue();
        _uowMock.Verify(x => x.Wishlist.Delete(item), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserWishlistAsync_ShouldReturnList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var book = new Book("T", "D", new ISBN("123"), new Money(1, "USD"), "A", 1, Guid.NewGuid());
        var items = new List<WishlistItem> { new WishlistItem(userId, book.Id) { Book = book } };

        _uowMock.Setup(x => x.Wishlist.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var result = await _service.GetUserWishlistAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task IsInWishlistAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _uowMock.Setup(x => x.Wishlist.ExistsAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsInWishlistAsync(userId, bookId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }
}
