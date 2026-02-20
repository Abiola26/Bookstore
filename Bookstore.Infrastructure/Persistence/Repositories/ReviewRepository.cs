using Bookstore.Domain.Entities;
using Bookstore.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(BookStoreDbContext context) : base(context) { }

    public async Task<ICollection<Review>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.BookId == bookId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<Review>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .Include(r => r.Book)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasUserReviewedBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(r => r.UserId == userId && r.BookId == bookId, cancellationToken);
    }

    public async Task<decimal> GetAverageRatingAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var ratings = await _dbSet
            .Where(r => r.BookId == bookId)
            .Select(r => (decimal)r.Rating)
            .ToListAsync(cancellationToken);

        if (!ratings.Any())
            return 0;

        return ratings.Average();
    }
}
