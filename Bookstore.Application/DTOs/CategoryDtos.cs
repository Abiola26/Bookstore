namespace Bookstore.Application.DTOs;

public class CategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
}

public class CategoryUpdateDto
{
    public string? Name { get; set; }
}

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BookCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
