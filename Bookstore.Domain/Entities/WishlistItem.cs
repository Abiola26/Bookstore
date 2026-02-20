using System;

namespace Bookstore.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    private WishlistItem() { }

    public WishlistItem(Guid userId, Guid bookId)
    {
        UserId = userId;
        BookId = bookId;
    }
}
