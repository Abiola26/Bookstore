using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Infrastructure.Services;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Tests.Unit.Infrastructure.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<OrderService>>();

        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Books).Returns(_bookRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _service = new OrderService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenEmailNotConfirmed_ShouldReturnForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("John Doe", "john@example.com", "hash", UserRole.User);
        user.EmailConfirmed = false;
        
        var dto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = Guid.NewGuid(), Quantity = 1 } }
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CreateOrderAsync(userId, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Contain("Email must be confirmed");
    }

    [Fact]
    public async Task CreateOrderAsync_WithIdempotencyKey_ShouldReturnResultIfExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var idempotencyKey = "key123";
        var existingOrder = new Order(userId) { IdempotencyKey = idempotencyKey };
        existingOrder.TotalAmount = new Money(100, "USD");
        
        var dto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = Guid.NewGuid(), Quantity = 1 } }
        };

        _orderRepositoryMock.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _service.CreateOrderAsync(userId, dto, idempotencyKey);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Order already exists");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WithIdempotencyKeyForDifferentUser_ShouldReturnConflict()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var idempotencyKey = "key123";
        var existingOrder = new Order(differentUserId) { IdempotencyKey = idempotencyKey };
        
        var dto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> { new OrderItemCreateDto { BookId = Guid.NewGuid(), Quantity = 1 } }
        };

        _orderRepositoryMock.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _service.CreateOrderAsync(userId, dto, idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Contain("Invalid idempotency key for this user");
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("John Doe", "john@example.com", "hash", UserRole.User) { EmailConfirmed = true };
        var book = new BookBuilder().Build();
        
        var dto = new OrderCreateDto
        {
            Items = new List<OrderItemCreateDto> 
            { 
                new OrderItemCreateDto { BookId = book.Id, Quantity = 1 } 
            }
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _bookRepositoryMock.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.OrderItems).Returns(new Mock<IOrderItemRepository>().Object);

        _orderRepositoryMock.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => 
            {
                var o = new Order(userId);
                // We could populate it more if needed for assertions
                return o;
            });

        // Act
        var result = await _service.CreateOrderAsync(userId, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        _orderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
