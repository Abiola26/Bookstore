using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using Bookstore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Services;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _service = new OrderService(_uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = new Order(userId) { Id = orderId };
        
        _uowMock.Setup(x => x.Orders.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetOrderByIdAsync(orderId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _uowMock.Setup(x => x.Orders.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderByIdAsync(orderId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be("Order not found");
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldReturnForbidden_WhenEmailNotConfirmed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new OrderCreateDto { Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = Guid.NewGuid(), Quantity = 1 } } };
        var user = new User("T", "e", "h", UserRole.User) { Id = userId, EmailConfirmed = false };

        _uowMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CreateOrderAsync(userId, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }
    [Fact]
    public async Task CancelOrderAsync_ShouldCancelOrder_WhenOrderExistsAndIsPending()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = new Order(userId) { Id = orderId, Status = OrderStatus.Pending };
        
        _uowMock.Setup(x => x.Orders.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.Success.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        _uowMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldUpdateStatus_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(Guid.NewGuid()) { Id = orderId, Status = OrderStatus.Pending };
        var dto = new OrderUpdateStatusDto { Status = "Paid" };

        _uowMock.Setup(x => x.Orders.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.UpdateOrderStatusAsync(orderId, dto);

        // Assert
        result.Success.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ShouldReturnOrders_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("T", "e", "h", UserRole.User) { Id = userId };
        var orders = new List<Order> { new Order(userId) };

        _uowMock.Setup(x => x.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _uowMock.Setup(x => x.Orders.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.GetUserOrdersAsync(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }
}
