namespace Bookstore.Application.DTOs;

public class OrderItemCreateDto
{
    public Guid BookId { get; set; }
    public int Quantity { get; set; }
}

public class OrderItemResponseDto
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal SubTotal => Quantity * UnitPrice;
}

public class OrderCreateDto
{
    public ICollection<OrderItemCreateDto> Items { get; set; } = new List<OrderItemCreateDto>();
}

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public ICollection<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class OrderUpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}
