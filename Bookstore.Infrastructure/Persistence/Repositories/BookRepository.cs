using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(BookStoreDbContext context) : base(context) { }

    public async Task<Book?> GetByISBNAsync(string isbn, CancellationToken cancellationToken = default)
    {
        // SqlQueryRaw bypasses the ISBN value-object converter so the column is compared as a plain string.
        var id = await _context.Database
            .SqlQueryRaw<Guid>(
                @"SELECT ""Id"" ""Value"" FROM ""Books"" WHERE ""ISBN"" = {0} LIMIT 1",
                isbn)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == Guid.Empty)
            return null;

        return await _dbSet.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
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
        var searchTerm = title.ToLower();
        var query = _dbSet
            .Where(b => b.Title.ToLower().Contains(searchTerm) || b.Author.ToLower().Contains(searchTerm));

        ISBN? searchIsbn = null;
        try 
        { 
            searchIsbn = new ISBN(title); 
            query = _dbSet
                .Where(b => b.Title.ToLower().Contains(searchTerm) || 
                            b.Author.ToLower().Contains(searchTerm) ||
                            b.ISBN == searchIsbn);
        } catch { }

        return await query
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
        var isbnObj = new ISBN(isbn);
        
        var query = _context.Books
            .AsNoTracking();

        if (excludeBookId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookId.Value);
        }

        return await query.AnyAsync(b => b.ISBN == isbnObj, cancellationToken);
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
