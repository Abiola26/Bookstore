using Bookstore.Domain.Entities;
using Bookstore.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(BookStoreDbContext context) : base(context) { }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (excludeCategoryId.HasValue)
            query = query.Where(c => c.Id != excludeCategoryId.Value);

        return await query.AnyAsync(c => c.Name == name, cancellationToken);
    }
}
