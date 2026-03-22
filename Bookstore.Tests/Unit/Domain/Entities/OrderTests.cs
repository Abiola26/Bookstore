using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;
using FluentAssertions;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class OrderTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();
    private readonly Money _price = new Money(10, "USD");

    [Fact]
    public void Constructor_ShouldInitializeWithPendingStatus()
    {
        // Act
        var order = new Order(_userId);

        // Assert
        order.UserId.Should().Be(_userId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Amount.Should().Be(0);
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ShouldAddItemAndIncreaseTotal()
    {
        // Arrange
        var order = new Order(_userId);
        var quantity = 2;

        // Act
        order.AddItem(_bookId, quantity, _price);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(quantity);
        order.TotalAmount.Amount.Should().Be(20);
    }

    [Fact]
    public void AddItem_ShouldThrowException_WhenOrderNotPending()
    {
        // Arrange
        var order = new Order(_userId);
        order.Status = OrderStatus.Processing;

        // Act
        Action act = () => order.AddItem(_bookId, 1, _price);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateStatus_ShouldUpdateStatus_WhenTransitionIsValid()
    {
        // Arrange
        var order = new Order(_userId);

        // Act
        order.UpdateStatus(OrderStatus.Processing);

        // Assert
        order.Status.Should().Be(OrderStatus.Processing);
    }
}
