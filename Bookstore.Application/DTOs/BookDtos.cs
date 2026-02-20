namespace Bookstore.Application.DTOs;

public class BookCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Author { get; set; } = string.Empty;
    public int Pages { get; set; }
    public string? Language { get; set; }
    public string? CoverImageUrl { get; set; }
    public int TotalQuantity { get; set; }
    public Guid CategoryId { get; set; }
}

public class BookUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Author { get; set; }
    public int? Pages { get; set; }
    public string? Language { get; set; }
    public string? CoverImageUrl { get; set; }
    public int? TotalQuantity { get; set; }
    public Guid? CategoryId { get; set; }
}

public class BookResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Author { get; set; } = string.Empty;
    public int Pages { get; set; }
    public string? Language { get; set; }
    public string? CoverImageUrl { get; set; }
    public int TotalQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

