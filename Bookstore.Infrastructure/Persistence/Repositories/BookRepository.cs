using Bookstore.Domain.Entities;
using Bookstore.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(BookStoreDbContext context) : base(context) { }

    public async Task<Book?> GetByISBNAsync(string isbn, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.ISBN.ToString() == isbn, cancellationToken);
    }

    public async Task<ICollection<Book>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.CategoryId == categoryId)
            .Include(b => b.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<Book>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Title.Contains(title))
            .Include(b => b.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<Book>> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Category)
            .OrderBy(b => b.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public async Task<bool> ISBNExistsAsync(string isbn, Guid? excludeBookId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        if (excludeBookId.HasValue)
            query = query.Where(b => b.Id != excludeBookId.Value);

        return await query.AnyAsync(b => b.ISBN.ToString() == isbn, cancellationToken);
    }

    public async Task<ICollection<Book>> GetPaginatedByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.CategoryId == categoryId)
            .Include(b => b.Category)
            .OrderBy(b => b.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCategoryBookCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(b => b.CategoryId == categoryId, cancellationToken);
    }
}
