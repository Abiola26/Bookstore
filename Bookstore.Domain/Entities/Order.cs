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
    public string? IdempotencyKey { get; set; }

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Order() { }

    public Order(Guid userId)
    {
        UserId = userId;
    }

    public void AddItem(OrderItem item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        
        _orderItems.Add(item);
        
        if (_orderItems.Count == 1)
        {
            TotalAmount = item.UnitPrice * item.Quantity;
        }
        else
        {
            TotalAmount = TotalAmount + (item.UnitPrice * item.Quantity);
        }
    }
}
