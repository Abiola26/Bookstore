using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class WishlistRepository : GenericRepository<WishlistItem>, IWishlistRepository
{
    public WishlistRepository(BookStoreDbContext context) : base(context)
    {
    }

    public async Task<ICollection<WishlistItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(w => w.Book)
            .ThenInclude(b => b.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WishlistItem?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(w => w.UserId == userId && w.BookId == bookId, cancellationToken);
    }
}
