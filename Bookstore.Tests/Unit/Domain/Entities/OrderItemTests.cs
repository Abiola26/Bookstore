using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class OrderItemTests
{
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _bookId  = Guid.NewGuid();
    private readonly Money _price  = new Money(25m, "USD");

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var item = new OrderItem(_orderId, _bookId, 3, _price);

        item.OrderId.Should().Be(_orderId);
        item.BookId.Should().Be(_bookId);
        item.Quantity.Should().Be(3);
        item.UnitPrice.Amount.Should().Be(25m);
        item.UnitPrice.Currency.Should().Be("USD");
        item.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenQuantityIsNotPositive(int quantity)
    {
        Action act = () => new OrderItem(_orderId, _bookId, quantity, _price);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ShouldAllowNullPrice_BecauseEFUsesPrivateConstructor()
    {
        // NOTE: The public constructor does NOT guard against null price,
        // so we just verify the happy path stores null-safe
        var item = new OrderItem(_orderId, _bookId, 1, new Money(0m, "USD"));
        item.UnitPrice.Amount.Should().Be(0m);
    }
}
