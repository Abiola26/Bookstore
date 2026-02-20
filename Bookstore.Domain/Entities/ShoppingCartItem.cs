using System;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Domain.Entities;

/// <summary>
/// Represents an item in a shopping cart
/// </summary>
public class ShoppingCartItem : BaseEntity
{
    public Guid ShoppingCartId { get; private set; }
    public ShoppingCart ShoppingCart { get; set; } = null!;

    public Guid BookId { get; private set; }
    public Book Book { get; set; } = null!;

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    private ShoppingCartItem()
    {
        UnitPrice = null!;
    }

    public ShoppingCartItem(Guid shoppingCartId, Guid bookId, int quantity, Money unitPrice)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than 0");
        if (unitPrice == null) throw new ArgumentNullException(nameof(unitPrice));

        ShoppingCartId = shoppingCartId;
        BookId = bookId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Updates the quantity of this cart item
    /// </summary>
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(newQuantity), "Quantity must be greater than 0");
        Quantity = newQuantity;
    }

    /// <summary>
    /// Gets the subtotal for this item
    /// </summary>
    public Money GetSubTotal() => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}
