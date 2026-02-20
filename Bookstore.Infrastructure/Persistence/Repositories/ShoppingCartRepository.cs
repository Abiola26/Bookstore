using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class ShoppingCartRepository : GenericRepository<ShoppingCart>, IShoppingCartRepository
{
    public ShoppingCartRepository(BookStoreDbContext context) : base(context)
    {
    }

    public async Task<ShoppingCart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ShoppingCarts
            .FirstOrDefaultAsync(sc => sc.UserId == userId && !sc.IsDeleted, cancellationToken);
    }

    public async Task<ShoppingCart?> GetWithItemsAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return await _context.ShoppingCarts
            .Include(sc => sc.Items)
            .ThenInclude(sci => sci.Book)
            .FirstOrDefaultAsync(sc => sc.Id == cartId && !sc.IsDeleted, cancellationToken);
    }

    public async Task<ShoppingCart?> GetUserCartWithItemsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ShoppingCarts
            .Include(sc => sc.Items)
            .ThenInclude(sci => sci.Book)
            .FirstOrDefaultAsync(sc => sc.UserId == userId && !sc.IsDeleted, cancellationToken);
    }
}
