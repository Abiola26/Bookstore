using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class ShoppingCartServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<ShoppingCartService>> _loggerMock;
    private readonly ShoppingCartService _service;

    public ShoppingCartServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ShoppingCartService>>();
        _service = new ShoppingCartService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserCartAsync_ShouldReturnEmptyCart_WhenCartDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _uowMock.Setup(x => x.ShoppingCarts.GetUserCartWithItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart?)null);

        // Act
        var result = await _service.GetUserCartAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.IsEmpty.Should().BeTrue();
        _uowMock.Verify(x => x.ShoppingCarts.AddAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddToCartAsync_ShouldAddItem_WhenStockIsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var dto = new AddToCartDto { BookId = bookId, Quantity = 2 };
        var cart = new ShoppingCart(userId) { Id = Guid.NewGuid() };
        var book = new Book("T", "D", new ISBN("123"), new Money(10, "USD"), "A", 10, Guid.NewGuid()) { Id = bookId };

        _uowMock.Setup(x => x.ShoppingCarts.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _uowMock.Setup(x => x.ShoppingCarts.GetWithItemsAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.AddToCartAsync(userId, dto);

        // Assert
        result.Success.Should().BeTrue();
        cart.Items.Should().HaveCount(1);
        cart.Items.First().Quantity.Should().Be(2);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddToCartAsync_ShouldReturnError_WhenStockIsInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var dto = new AddToCartDto { BookId = bookId, Quantity = 20 };
        var cart = new ShoppingCart(userId) { Id = Guid.NewGuid() };
        var book = new Book("T", "D", new ISBN("123"), new Money(10, "USD"), "A", 10, Guid.NewGuid()) { Id = bookId };

        _uowMock.Setup(x => x.ShoppingCarts.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        var result = await _service.AddToCartAsync(userId, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Insufficient stock for this book");
    }
    [Fact]
    public async Task UpdateCartItemAsync_ShouldUpdateQuantity_WhenStockIsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cart = new ShoppingCart(userId) { Id = Guid.NewGuid() };
        var bookPrice = new Money(10, "USD");
        var cartItem = new ShoppingCartItem(cart.Id, bookId, 1, bookPrice) { Id = cartItemId };
        cart.AddItem(cartItem);
        
        var book = new Book("T", "D", new ISBN("123"), bookPrice, "A", 10, Guid.NewGuid()) { Id = bookId };
        var dto = new UpdateCartItemDto { Quantity = 5 };

        _uowMock.Setup(x => x.ShoppingCarts.GetUserCartWithItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uowMock.Setup(x => x.Books.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _uowMock.Setup(x => x.ShoppingCarts.GetWithItemsAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.UpdateCartItemAsync(userId, cartItemId, dto);

        // Assert
        result.Success.Should().BeTrue();
        cart.Items.First().Quantity.Should().Be(5);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromCartAsync_ShouldRemoveItem_WhenItemExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var cart = new ShoppingCart(userId) { Id = Guid.NewGuid() };
        var cartItem = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 1, new Money(10, "USD")) { Id = cartItemId };
        cart.AddItem(cartItem);

        _uowMock.Setup(x => x.ShoppingCarts.GetUserCartWithItemsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uowMock.Setup(x => x.ShoppingCarts.GetWithItemsAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.RemoveFromCartAsync(userId, cartItemId);

        // Assert
        result.Success.Should().BeTrue();
        cart.Items.Should().BeEmpty();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldEmptyCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId) { Id = Guid.NewGuid() };
        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 1, new Money(10, "USD")));

        _uowMock.Setup(x => x.ShoppingCarts.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.ClearCartAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        cart.Items.Should().BeEmpty();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
