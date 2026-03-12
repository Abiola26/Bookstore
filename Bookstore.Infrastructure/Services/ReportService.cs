using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Microsoft.Extensions.Logging;

namespace Bookstore.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<DashboardReportDto>> GetDashboardReportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalOrders = await _unitOfWork.Orders.GetTotalOrderCountAsync(cancellationToken);
            var totalBooks = await _unitOfWork.Books.GetTotalCountAsync(cancellationToken);
            var totalUsers = (await _unitOfWork.Users.GetAllAsync(cancellationToken)).Count;
            var totalRevenue = await _unitOfWork.Reports.GetTotalRevenueAsync(cancellationToken);
            
            var topBooks = await _unitOfWork.Reports.GetTopSellingBooksAsync(5, cancellationToken);
            var monthlySales = await _unitOfWork.Reports.GetMonthlySalesAsync(6, cancellationToken);
            var salesByCategory = await _unitOfWork.Reports.GetSalesByCategoryAsync(cancellationToken);
            
            // Get 10 most recent orders
            var recentOrders = await _unitOfWork.Orders.GetAllOrdersPaginatedAsync(1, 10, cancellationToken);
            
            var report = new DashboardReportDto
            {
                TotalOrders = totalOrders,
                TotalBooks = totalBooks,
                TotalUsers = totalUsers,
                TotalRevenue = totalRevenue,
                TopSellingBooks = topBooks.ToList(),
                MonthlySales = monthlySales.ToList(),
                SalesByCategory = salesByCategory.ToList(),
                RecentSales = recentOrders.Select(o => new SalesSummaryDto
                {
                    OrderId = o.Id,
                    CustomerName = o.User?.FullName ?? "Unknown",
                    Amount = o.TotalAmount.Amount,
                    Status = o.Status.ToString(),
                    Date = o.CreatedAt
                }).ToList()
            };

            return ApiResponse<DashboardReportDto>.SuccessResponse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard report");
            return ApiResponse<DashboardReportDto>.ErrorResponse("Failed to generate dashboard report");
        }
    }

    public async Task<ApiResponse<InventoryReportDto>> GetInventoryReportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allBooks = await _unitOfWork.Books.GetAllAsync(cancellationToken);
            var lowStockBooks = allBooks
                .Where(b => b.TotalQuantity < 10)
                .Select(b => new LowStockBookDto
                {
                    BookId = b.Id,
                    Title = b.Title,
                    CurrentStock = b.TotalQuantity
                })
                .ToList();

            var stockByCategory = await _unitOfWork.Reports.GetStockByCategoryAsync(cancellationToken);

            var report = new InventoryReportDto
            {
                TotalStock = allBooks.Sum(b => b.TotalQuantity),
                LowStockBooks = lowStockBooks,
                StockByCategory = stockByCategory.ToList()
            };

            return ApiResponse<InventoryReportDto>.SuccessResponse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory report");
            return ApiResponse<InventoryReportDto>.ErrorResponse("Failed to generate inventory report");
        }
    }

    public async Task<ApiResponse<byte[]>> ExportSalesReportAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        // For now, return a simple CSV mock or implementation
        // In a real app, this would use a library like CsvHelper or ClosedXML
        try
        {
            var orders = await _unitOfWork.Orders.GetAllAsync(cancellationToken);
            var filteredOrders = orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            
            await writer.WriteLineAsync("OrderId,Date,Customer,Amount,Status");
            foreach (var order in filteredOrders)
            {
                // Note: In a production app, we'd ensure User is loaded in the repository call
                await writer.WriteLineAsync($"{order.Id},{order.CreatedAt:yyyy-MM-dd HH:mm},{order.UserId},{order.TotalAmount.Amount},{order.Status}");
            }
            await writer.FlushAsync();
            
            return ApiResponse<byte[]>.SuccessResponse(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report");
            return ApiResponse<byte[]>.ErrorResponse("Failed to export sales report");
        }
    }

    public async Task<ApiResponse<UserEngagementReportDto>> GetUserEngagementReportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalUsers = (await _unitOfWork.Users.GetAllAsync(cancellationToken)).Count;
            var activeThreshold = DateTimeOffset.UtcNow.AddDays(-30);
            var activeUsersCount = (await _unitOfWork.Orders.GetAllAsync(cancellationToken))
                .Where(o => o.CreatedAt >= activeThreshold)
                .Select(o => o.UserId)
                .Distinct()
                .Count();

            var topCustomers = await _unitOfWork.Reports.GetTopCustomersAsync(10, cancellationToken);
            var userGrowth = await _unitOfWork.Reports.GetUserGrowthAsync(12, cancellationToken);

            var report = new UserEngagementReportDto
            {
                TotalRegisteredUsers = totalUsers,
                ActiveUsersLast30Days = activeUsersCount,
                TopCustomers = topCustomers.ToList(),
                UserGrowth = userGrowth.ToList()
            };

            return ApiResponse<UserEngagementReportDto>.SuccessResponse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user engagement report");
            return ApiResponse<UserEngagementReportDto>.ErrorResponse("Failed to generate user engagement report");
        }
    }

    public async Task<ApiResponse<ReviewAnalyticsReportDto>> GetReviewAnalyticsReportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allReviews = await _unitOfWork.Reviews.GetAllAsync(cancellationToken);
            var avgRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
            
            var distribution = await _unitOfWork.Reports.GetRatingDistributionAsync(cancellationToken);
            var topRatedBooks = await _unitOfWork.Reports.GetTopRatedBooksAsync(5, cancellationToken);

            var report = new ReviewAnalyticsReportDto
            {
                AverageRating = Math.Round(avgRating, 2),
                TotalReviews = allReviews.Count,
                RatingDistribution = distribution.ToList(),
                TopRatedBooks = topRatedBooks.ToList()
            };

            return ApiResponse<ReviewAnalyticsReportDto>.SuccessResponse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating review analytics report");
            return ApiResponse<ReviewAnalyticsReportDto>.ErrorResponse("Failed to generate review analytics report");
        }
    }
}
