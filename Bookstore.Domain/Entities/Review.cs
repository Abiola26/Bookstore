using System;

namespace Bookstore.Domain.Entities;

public class Review : BaseEntity
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;

    private Review() { }

    public Review(Guid bookId, Guid userId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        
        if (string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("Comment cannot be empty.", nameof(comment));

        BookId = bookId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
    }
}
