using System;
using System.Collections.Generic;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Domain.Entities;

/// <summary>
/// Represents a shopping cart for temporary storage of items before checkout
/// Provides cart management functionality
/// </summary>
public class ShoppingCart : BaseEntity
{
    private readonly List<ShoppingCartItem> _items = new();

    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;

    public Money TotalPrice { get; set; } = Money.Zero();

    public DateTimeOffset LastModified { get; set; }

    public IReadOnlyCollection<ShoppingCartItem> Items => _items.AsReadOnly();

    private ShoppingCart() { }

    public ShoppingCart(Guid userId)
    {
        UserId = userId;
        LastModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds or updates an item in the shopping cart
    /// </summary>
    public void AddItem(ShoppingCartItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var existingItem = _items.FirstOrDefault(i => i.BookId == item.BookId);
        
        if (existingItem != null)
        {
            // Update quantity if item already exists
            existingItem.UpdateQuantity(existingItem.Quantity + item.Quantity);
        }
        else
        {
            _items.Add(item);
        }

        RecalculateTotal();
        LastModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes an item from the shopping cart
    /// </summary>
    public void RemoveItem(Guid cartItemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
            LastModified = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Updates the quantity of a cart item
    /// </summary>
    public void UpdateItemQuantity(Guid cartItemId, int newQuantity)
    {
        if (newQuantity < 1) throw new ArgumentOutOfRangeException(nameof(newQuantity), "Quantity must be at least 1");

        var item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            item.UpdateQuantity(newQuantity);
            RecalculateTotal();
            LastModified = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Clears all items from the shopping cart
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        TotalPrice = Money.Zero();
        LastModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Recalculates the total price of all items in the cart
    /// </summary>
    private void RecalculateTotal()
    {
        var total = _items.Sum(i => (i.UnitPrice.Amount * i.Quantity));
        var currency = _items.FirstOrDefault()?.UnitPrice.Currency ?? "USD";
        TotalPrice = new Money(total, currency);
    }

    /// <summary>
    /// Gets the number of items in the cart
    /// </summary>
    public int GetItemCount() => _items.Count;

    /// <summary>
    /// Checks if the cart is empty
    /// </summary>
    public bool IsEmpty => _items.Count == 0;
}
