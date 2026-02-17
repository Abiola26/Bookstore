using System;
using System.Collections.Generic;
using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _orderItems = new();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Money TotalAmount { get; set; } = Money.Zero();

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Order() { }

    public Order(Guid userId)
    {
        UserId = userId;
    }

    public void AddItem(OrderItem item)
    {
        _orderItems.Add(item);
        TotalAmount = new Money(TotalAmount.Amount + (item.UnitPrice * item.Quantity).Amount, TotalAmount.Currency);
    }
}
