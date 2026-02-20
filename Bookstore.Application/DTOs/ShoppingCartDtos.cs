namespace Bookstore.Application.DTOs;

/// <summary>
/// DTO for adding an item to the shopping cart
/// </summary>
public class AddToCartDto
{
    public Guid BookId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// DTO for updating an item quantity in the shopping cart
/// </summary>
public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

/// <summary>
/// DTO representing a shopping cart item response
/// </summary>
public class ShoppingCartItemResponseDto
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

/// <summary>
/// DTO representing the complete shopping cart
/// </summary>
public class ShoppingCartResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int ItemCount { get; set; }
    public bool IsEmpty { get; set; }
    public ICollection<ShoppingCartItemResponseDto> Items { get; set; } = new List<ShoppingCartItemResponseDto>();
    public DateTimeOffset LastModified { get; set; }
}

/// <summary>
/// DTO for clearing the shopping cart
/// </summary>
public class ClearCartDto
{
    public bool ConfirmClear { get; set; }
}
