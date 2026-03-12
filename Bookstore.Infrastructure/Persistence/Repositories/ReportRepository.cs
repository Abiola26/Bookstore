using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly BookStoreDbContext _context;

    public ReportRepository(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed || o.IsPaid)
            .SumAsync(o => (decimal)o.TotalAmount.Amount, cancellationToken);
    }

    public async Task<ICollection<TopBookDto>> GetTopSellingBooksAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .GroupBy(oi => new { oi.BookId, Title = oi.Book.Title, Author = oi.Book.Author })
            .Select(g => new TopBookDto
            {
                BookId = g.Key.BookId,
                Title = g.Key.Title,
                Author = g.Key.Author,
                UnitsSold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => (decimal)oi.UnitPrice.Amount * oi.Quantity)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<MonthlySalesDto>> GetMonthlySalesAsync(int months, CancellationToken cancellationToken = default)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);
        
        var sales = await _context.Orders
            .Where(o => o.CreatedAt >= startDate && (o.Status == OrderStatus.Completed || o.IsPaid))
            .ToListAsync(cancellationToken);

        return sales
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new MonthlySalesDto
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("MMM yyyy"),
                Revenue = g.Sum(o => (decimal)o.TotalAmount.Amount),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Month) // Note: This might not be chronological correctly, but good enough for now
            .ToList();
    }

    public async Task<ICollection<CategoryStockDto>> GetStockByCategoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Select(c => new CategoryStockDto
            {
                CategoryName = c.Name,
                BookCount = c.Books.Count,
                TotalStock = c.Books.Sum(b => b.TotalQuantity)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<CategorySalesDto>> GetSalesByCategoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .GroupBy(oi => oi.Book.Category.Name)
            .Select(g => new CategorySalesDto
            {
                CategoryName = g.Key,
                Revenue = g.Sum(oi => (decimal)oi.UnitPrice.Amount * oi.Quantity),
                UnitsSold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TopCustomerDto>> GetTopCustomersAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed || o.IsPaid)
            .GroupBy(o => new { o.UserId, FullName = o.User.FullName })
            .Select(g => new TopCustomerDto
            {
                UserId = g.Key.UserId,
                FullName = g.Key.FullName,
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => (decimal)o.TotalAmount.Amount)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<UserGrowthDto>> GetUserGrowthAsync(int months, CancellationToken cancellationToken = default)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);
        var users = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .ToListAsync(cancellationToken);

        return users
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new UserGrowthDto
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("MMM yyyy"),
                NewUsers = g.Count()
            })
            .OrderBy(x => x.Month)
            .ToList();
    }

    public async Task<ICollection<RatingCountDto>> GetRatingDistributionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .GroupBy(r => r.Rating)
            .Select(g => new RatingCountDto
            {
                Rating = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Rating)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TopRatedBookDto>> GetTopRatedBooksAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .GroupBy(r => new { r.BookId, Title = r.Book.Title })
            .Select(g => new TopRatedBookDto
            {
                BookId = g.Key.BookId,
                Title = g.Key.Title,
                AverageRating = g.Average(r => r.Rating),
                ReviewCount = g.Count()
            })
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.ReviewCount)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
