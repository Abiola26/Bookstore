using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.DTOs;

public class ReviewCreateDto
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required]
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string Comment { get; set; } = string.Empty;
}

public class ReviewUpdateDto
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int? Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string? Comment { get; set; }
}

public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class BookReviewSummaryDto
{
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public ICollection<ReviewResponseDto> RecentReviews { get; set; } = new List<ReviewResponseDto>();
}
