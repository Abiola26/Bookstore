using AutoMapper;
using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Features.Orders.Commands;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bookstore.Tests.Unit.Application.Features.Orders.Commands;

public class CreateOrderHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _handler = new CreateOrderHandler(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateOrderCommand(userId, new OrderCreateDto { ShippingAddress = "Test" });
        
        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", EmailConfirmed = false };
        var command = new CreateOrderCommand(userId, new OrderCreateDto { ShippingAddress = "Test" });

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email must be confirmed");
        result.StatusCode.Should().Be(403);
    }
}
