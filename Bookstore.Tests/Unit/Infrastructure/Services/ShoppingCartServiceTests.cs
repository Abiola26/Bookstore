using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Services;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for ShoppingCartService
/// Tests business logic including validation, stock checks, and cart operations
/// </summary>
public class ShoppingCartServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IShoppingCartRepository> _cartRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<ILogger<ShoppingCartService>> _loggerMock;
    private readonly ShoppingCartService _service;

    public ShoppingCartServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cartRepositoryMock = new Mock<IShoppingCartRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _loggerMock = new Mock<ILogger<ShoppingCartService>>();

        _unitOfWorkMock.Setup(u => u.ShoppingCarts).Returns(_cartRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Books).Returns(_bookRepositoryMock.Object);

        _service = new ShoppingCartService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserCartAsync_WithExistingCart_ShouldReturnCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetUserCartWithItemsAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        // Act
        var result = await _service.GetUserCartAsync(userId, cancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        _cartRepositoryMock.Verify(r => r.GetUserCartWithItemsAsync(userId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetUserCartAsync_WithoutExistingCart_ShouldCreateNewCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetUserCartWithItemsAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(null));

        _cartRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ShoppingCart>(), cancellationToken))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(cancellationToken))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.GetUserCartAsync(userId, cancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        _cartRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ShoppingCart>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_WithValidData_ShouldAddItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var book = new BookBuilder().Build();
        var dto = new AddToCartDto { BookId = bookId, Quantity = 2 };
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Returns(Task.FromResult<Book?>(book));

        _cartRepositoryMock
            .Setup(r => r.Update(It.IsAny<ShoppingCart>()))
            .Callback<ShoppingCart>(c => { });

        _cartRepositoryMock
            .Setup(r => r.GetWithItemsAsync(cart.Id, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(cancellationToken))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.AddToCartAsync(userId, dto, cancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        _cartRepositoryMock.Verify(r => r.Update(It.IsAny<ShoppingCart>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_WithInvalidQuantity_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new AddToCartDto { BookId = Guid.NewGuid(), Quantity = 0 };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _service.AddToCartAsync(userId, dto, cancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Quantity must be greater than 0");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task AddToCartAsync_WithNonexistentBook_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var dto = new AddToCartDto { BookId = bookId, Quantity = 2 };
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Returns(Task.FromResult<Book?>(null));

        // Act
        var result = await _service.AddToCartAsync(userId, dto, cancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Book not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddToCartAsync_WithDeletedBook_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var book = new BookBuilder().Build();
        book.IsDeleted = true;  // Soft delete
        var dto = new AddToCartDto { BookId = bookId, Quantity = 2 };
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Returns(Task.FromResult<Book?>(book));

        // Act
        var result = await _service.AddToCartAsync(userId, dto, cancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("no longer available");
        result.StatusCode.Should().Be(410);
    }

    [Fact]
    public async Task AddToCartAsync_WithInsufficientStock_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var book = new BookBuilder().WithTotalQuantity(2).Build();
        var dto = new AddToCartDto { BookId = bookId, Quantity = 5 };
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Returns(Task.FromResult<Book?>(book));

        // Act
        var result = await _service.AddToCartAsync(userId, dto, cancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient stock");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateCartItemAsync_WithValidData_ShouldUpdateQuantity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var book = new BookBuilder().Build();
        var cartItem = new ShoppingCartItem(cart.Id, bookId, 2, book.Price);
        cart.AddItem(cartItem);
        var dto = new UpdateCartItemDto { Quantity = 5 };
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetUserCartWithItemsAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _bookRepositoryMock
            .Setup(r => r.GetByIdAsync(bookId, cancellationToken))
            .Returns(Task.FromResult<Book?>(book));

        _cartRepositoryMock
            .Setup(r => r.Update(It.IsAny<ShoppingCart>()))
            .Callback<ShoppingCart>(c => { });

        _cartRepositoryMock
            .Setup(r => r.GetWithItemsAsync(cart.Id, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(cancellationToken))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.UpdateCartItemAsync(userId, cartItem.Id, dto, cancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        _cartRepositoryMock.Verify(r => r.Update(It.IsAny<ShoppingCart>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromCartAsync_WithValidId_ShouldRemoveItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var cartItem = new ShoppingCartItem(cart.Id, bookId, 2, new Money(10m, "USD"));
        cart.AddItem(cartItem);
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetUserCartWithItemsAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _cartRepositoryMock
            .Setup(r => r.Update(It.IsAny<ShoppingCart>()))
            .Callback<ShoppingCart>(c => { });

        _cartRepositoryMock
            .Setup(r => r.GetWithItemsAsync(cart.Id, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(cancellationToken))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.RemoveFromCartAsync(userId, cartItem.Id, cancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        _cartRepositoryMock.Verify(r => r.Update(It.IsAny<ShoppingCart>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromCartAsync_WithNonexistentItem_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetUserCartWithItemsAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        // Act
        var result = await _service.RemoveFromCartAsync(userId, Guid.NewGuid(), cancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Item not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldRemoveAllItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 2, new Money(10m, "USD")));
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(cart));

        _cartRepositoryMock
            .Setup(r => r.Update(It.IsAny<ShoppingCart>()))
            .Callback<ShoppingCart>(c => { });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(cancellationToken))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _service.ClearCartAsync(userId, cancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ClearCartAsync_WithNonexistentCart_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _cartRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, cancellationToken))
            .Returns(Task.FromResult<ShoppingCart?>(null));

        // Act
        var result = await _service.ClearCartAsync(userId, cancellationToken);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Shopping cart not found");
        result.StatusCode.Should().Be(404);
    }
}
