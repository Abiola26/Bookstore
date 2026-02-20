using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Tests.Builders;
using FluentAssertions;
using Xunit;

namespace Bookstore.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for ShoppingCart entity
/// Tests business logic and validation rules for cart operations
/// </summary>
public class ShoppingCartEntityTests
{
    [Fact]
    public void CreateShoppingCart_WithValidUserId_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var cart = new ShoppingCart(userId);

        // Assert
        cart.UserId.Should().Be(userId);
        cart.Id.Should().NotBeEmpty();
        cart.IsEmpty.Should().BeTrue();
        cart.GetItemCount().Should().Be(0);
        cart.TotalPrice.Amount.Should().Be(0);
        cart.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        cart.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void AddItem_WithValidItem_ShouldAddItemToCart()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var quantity = 2;
        var price = new Money(25.99m, "USD");
        var item = new ShoppingCartItem(cart.Id, bookId, quantity, price);

        // Act
        cart.AddItem(item);

        // Assert
        cart.GetItemCount().Should().Be(1);
        cart.IsEmpty.Should().BeFalse();
        cart.TotalPrice.Amount.Should().Be(51.98m);
        cart.Items.Should().Contain(i => i.BookId == bookId);
    }

    [Fact]
    public void AddItem_WithDuplicateBook_ShouldMergeQuantity()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var price = new Money(10m, "USD");
        var item1 = new ShoppingCartItem(cart.Id, bookId, 2, price);
        var item2 = new ShoppingCartItem(cart.Id, bookId, 3, price);

        // Act
        cart.AddItem(item1);
        cart.AddItem(item2);

        // Assert
        cart.GetItemCount().Should().Be(1); // Still one unique item
        var cartItem = cart.Items.First(i => i.BookId == bookId);
        cartItem.Quantity.Should().Be(5); // 2 + 3
        cart.TotalPrice.Amount.Should().Be(50m); // 5 * 10
    }

    [Fact]
    public void AddItem_WithNullItem_ShouldThrow()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => cart.AddItem(null!));
        exception.ParamName.Should().Be("item");
    }

    [Fact]
    public void RemoveItem_WithValidId_ShouldRemoveItem()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var item = new ShoppingCartItem(cart.Id, bookId, 2, new Money(15m, "USD"));
        cart.AddItem(item);
        var itemId = cart.Items.First().Id;

        // Act
        cart.RemoveItem(itemId);

        // Assert
        cart.GetItemCount().Should().Be(0);
        cart.IsEmpty.Should().BeTrue();
        cart.TotalPrice.Amount.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_WithInvalidId_ShouldNotThrow()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var invalidId = Guid.NewGuid();

        // Act & Assert - Should not throw, just do nothing
        cart.RemoveItem(invalidId);
        cart.GetItemCount().Should().Be(0);
    }

    [Fact]
    public void UpdateItemQuantity_WithValidQuantity_ShouldUpdateQuantity()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var item = new ShoppingCartItem(cart.Id, bookId, 2, new Money(20m, "USD"));
        cart.AddItem(item);
        var itemId = cart.Items.First().Id;

        // Act
        cart.UpdateItemQuantity(itemId, 5);

        // Assert
        var updatedItem = cart.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.Should().Be(5);
        cart.TotalPrice.Amount.Should().Be(100m); // 5 * 20
    }

    [Fact]
    public void UpdateItemQuantity_WithZeroQuantity_ShouldThrow()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var item = new ShoppingCartItem(cart.Id, bookId, 2, new Money(15m, "USD"));
        cart.AddItem(item);
        var itemId = cart.Items.First().Id;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            cart.UpdateItemQuantity(itemId, 0));
        exception.ParamName.Should().Be("newQuantity");
    }

    [Fact]
    public void UpdateItemQuantity_WithNegativeQuantity_ShouldThrow()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var item = new ShoppingCartItem(cart.Id, bookId, 2, new Money(15m, "USD"));
        cart.AddItem(item);
        var itemId = cart.Items.First().Id;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            cart.UpdateItemQuantity(itemId, -5));
        exception.ParamName.Should().Be("newQuantity");
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 2, new Money(10m, "USD")));
        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 3, new Money(15m, "USD")));

        // Act
        cart.Clear();

        // Assert
        cart.IsEmpty.Should().BeTrue();
        cart.GetItemCount().Should().Be(0);
        cart.TotalPrice.Amount.Should().Be(0);
    }

    [Fact]
    public void GetItemCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());

        // Act & Assert - Empty cart
        cart.GetItemCount().Should().Be(0);

        // Add items
        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 2, new Money(10m, "USD")));
        cart.GetItemCount().Should().Be(1);

        cart.AddItem(new ShoppingCartItem(cart.Id, Guid.NewGuid(), 3, new Money(15m, "USD")));
        cart.GetItemCount().Should().Be(2);
    }

    [Fact]
    public void LastModified_ShouldUpdateOnOperations()
    {
        // Arrange
        var cart = new ShoppingCart(Guid.NewGuid());
        var initialTime = cart.LastModified;

        // Act - Wait a moment and add item
        System.Threading.Thread.Sleep(10);
        var item = new ShoppingCartItem(cart.Id, Guid.NewGuid(), 1, new Money(10m, "USD"));
        cart.AddItem(item);

        // Assert
        cart.LastModified.Should().BeAfter(initialTime);
    }
}

/// <summary>
/// Unit tests for ShoppingCartItem entity
/// </summary>
public class ShoppingCartItemEntityTests
{
    [Fact]
    public void CreateShoppingCartItem_WithValidData_ShouldSucceed()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var quantity = 5;
        var unitPrice = new Money(25.99m, "USD");

        // Act
        var item = new ShoppingCartItem(cartId, bookId, quantity, unitPrice);

        // Assert
        item.ShoppingCartId.Should().Be(cartId);
        item.BookId.Should().Be(bookId);
        item.Quantity.Should().Be(quantity);
        item.UnitPrice.Should().Be(unitPrice);
        item.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateShoppingCartItem_WithZeroQuantity_ShouldThrow()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShoppingCartItem(cartId, bookId, 0, new Money(10m, "USD")));
        exception.ParamName.Should().Be("quantity");
    }

    [Fact]
    public void CreateShoppingCartItem_WithNegativeQuantity_ShouldThrow()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShoppingCartItem(cartId, bookId, -5, new Money(10m, "USD")));
        exception.ParamName.Should().Be("quantity");
    }

    [Fact]
    public void CreateShoppingCartItem_WithNullPrice_ShouldThrow()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ShoppingCartItem(cartId, bookId, 5, null!));
        exception.ParamName.Should().Be("unitPrice");
    }

    [Fact]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateSuccessfully()
    {
        // Arrange
        var item = new ShoppingCartItem(Guid.NewGuid(), Guid.NewGuid(), 5, new Money(10m, "USD"));

        // Act
        item.UpdateQuantity(10);

        // Assert
        item.Quantity.Should().Be(10);
    }

    [Fact]
    public void UpdateQuantity_WithZeroQuantity_ShouldThrow()
    {
        // Arrange
        var item = new ShoppingCartItem(Guid.NewGuid(), Guid.NewGuid(), 5, new Money(10m, "USD"));

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdateQuantity(0));
        exception.ParamName.Should().Be("newQuantity");
    }

    [Fact]
    public void GetSubTotal_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = new ShoppingCartItem(Guid.NewGuid(), Guid.NewGuid(), 5, new Money(10m, "USD"));

        // Act
        var subTotal = item.GetSubTotal();

        // Assert
        subTotal.Amount.Should().Be(50m);
        subTotal.Currency.Should().Be("USD");
    }
}
