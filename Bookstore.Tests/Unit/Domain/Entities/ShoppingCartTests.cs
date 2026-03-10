using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.Entities;

public class ShoppingCartTests
{
    [Fact]
    public void AddItem_ShouldAddFirstItemAndSetTotal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var unitPrice = new Money(10.5m, "USD");
        var item = new ShoppingCartItem(cartId, Guid.NewGuid(), 2, unitPrice);

        // Act
        cart.AddItem(item);

        // Assert
        cart.Items.Should().HaveCount(1);
        cart.TotalPrice.Amount.Should().Be(21.0m);
        cart.TotalPrice.Currency.Should().Be("USD");
        cart.IsEmpty.Should().BeFalse();
        cart.GetItemCount().Should().Be(1);
    }

    [Fact]
    public void AddItem_ShouldUpdateQuantity_WhenSameBookExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var unitPrice = new Money(10m, "USD");
        var item1 = new ShoppingCartItem(cartId, bookId, 1, unitPrice);
        var item2 = new ShoppingCartItem(cartId, bookId, 2, unitPrice);

        // Act
        cart.AddItem(item1);
        cart.AddItem(item2);

        // Assert
        cart.Items.Should().HaveCount(1);
        cart.Items.First().Quantity.Should().Be(3);
        cart.TotalPrice.Amount.Should().Be(30m);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItemAndStoreCorrectTotal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var item1 = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 1, new Money(10, "USD")) { Id = Guid.NewGuid() };
        var item2 = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 2, new Money(5, "USD")) { Id = Guid.NewGuid() };
        cart.AddItem(item1);
        cart.AddItem(item2);

        // Act
        cart.RemoveItem(item1.Id);

        // Assert
        cart.Items.Should().HaveCount(1);
        cart.TotalPrice.Amount.Should().Be(10.0m);
    }

    [Fact]
    public void UpdateItemQuantity_ShouldUpdateAndRecalculateTotal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = new ShoppingCart(userId);
        var item = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 1, new Money(10, "USD")) { Id = Guid.NewGuid() };
        cart.AddItem(item);

        // Act
        cart.UpdateItemQuantity(item.Id, 5);

        // Assert
        cart.Items.First().Quantity.Should().Be(5);
        cart.TotalPrice.Amount.Should().Be(50m);
    }

    [Fact]
    public void Clear_ShouldEmptyTheCart()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 1, new Money(1, "USD")));

        // Act
        cart.Clear();

        // Assert
        cart.Items.Should().BeEmpty();
        cart.TotalPrice.Amount.Should().Be(0);
        cart.IsEmpty.Should().BeTrue();
    }
}
