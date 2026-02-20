using Bookstore.Domain.Entities;

namespace Bookstore.Application.Repositories;

public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ICollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IBookRepository : IGenericRepository<Book>
{
    Task<Book?> GetByISBNAsync(string isbn, CancellationToken cancellationToken = default);
    Task<ICollection<Book>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<ICollection<Book>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<ICollection<Book>> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ISBNExistsAsync(string isbn, Guid? excludeBookId = null, CancellationToken cancellationToken = default);
    Task<ICollection<Book>> GetPaginatedByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCategoryBookCountAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<ICollection<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ICollection<Order>> GetByUserIdPaginatedAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUserOrderCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Order?> GetWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ICollection<Order>> GetAllOrdersPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalOrderCountAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
}

public interface IOrderItemRepository : IGenericRepository<OrderItem>
{
    Task<ICollection<OrderItem>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public interface IReviewRepository : IGenericRepository<Review>
{
    Task<ICollection<Review>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<ICollection<Review>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasUserReviewedBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
    Task<decimal> GetAverageRatingAsync(Guid bookId, CancellationToken cancellationToken = default);
}

public interface IWishlistRepository : IGenericRepository<WishlistItem>
{
    Task<ICollection<WishlistItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WishlistItem?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
}

public interface IShoppingCartRepository : IGenericRepository<ShoppingCart>
{
    Task<ShoppingCart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetWithItemsAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetUserCartWithItemsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    IBookRepository Books { get; }
    ICategoryRepository Categories { get; }
    IUserRepository Users { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    IReviewRepository Reviews { get; }
    IWishlistRepository Wishlist { get; }
    IShoppingCartRepository ShoppingCarts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
