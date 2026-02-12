using Bookstore.Domain.Entities;
using Bookstore.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(BookStoreDbContext context) : base(context) { }

    public async Task<ICollection<OrderItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(oi => oi.OrderId == orderId)
            .Include(oi => oi.Book)
            .ToListAsync(cancellationToken);
    }
}
