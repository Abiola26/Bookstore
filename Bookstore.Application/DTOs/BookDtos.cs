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
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class BookPaginatedResponseDto
{
    public ICollection<BookResponseDto> Data { get; set; } = new List<BookResponseDto>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
