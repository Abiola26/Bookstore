using Bookstore.Domain.ValueObjects;
using System;

namespace Bookstore.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    public Guid BookId { get; private set; }
    public Book Book { get; private set; } = null!;

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    private OrderItem()
    {
        UnitPrice = null!;
    }

    public OrderItem(Guid orderId, Guid bookId, int quantity, Money unitPrice)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        OrderId = orderId;
        BookId = bookId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
