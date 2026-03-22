using Bookstore.Domain.Enum;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Money TotalAmount { get; set; } = Money.Zero();
    public Money ShippingFee { get; set; } = Money.Zero();
    public string ShippingAddress { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public string? PaymentReference { get; set; }
    public bool IsPaid { get; set; } = false;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? IdempotencyKey { get; set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order() { }

    public Order(Guid userId)
    {
        UserId = userId;
    }

    public void AddItem(OrderItem item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        _items.Add(item);
        TotalAmount = TotalAmount + (item.UnitPrice * item.Quantity);
    }

    public void AddItem(Guid bookId, int quantity, Money price)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to a non-pending order");

        var item = new OrderItem(Id, bookId, quantity, price);
        _items.Add(item);
        TotalAmount = TotalAmount + (price * quantity);
    }

    public void UpdateStatus(OrderStatus status)
    {
        Status = status;
    }
}
