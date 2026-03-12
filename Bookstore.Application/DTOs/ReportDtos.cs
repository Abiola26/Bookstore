using Bookstore.Application.Common;

namespace Bookstore.Application.DTOs;

public class DashboardReportDto
{
    public int TotalOrders { get; set; }
    public int TotalBooks { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<SalesSummaryDto> RecentSales { get; set; } = new();
    public List<TopBookDto> TopSellingBooks { get; set; } = new();
    public List<MonthlySalesDto> MonthlySales { get; set; } = new();
    public List<CategorySalesDto> SalesByCategory { get; set; } = new();
}

public class CategorySalesDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int UnitsSold { get; set; }
}

public class SalesSummaryDto
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
}

public class TopBookDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MonthlySalesDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class InventoryReportDto
{
    public int TotalStock { get; set; }
    public List<LowStockBookDto> LowStockBooks { get; set; } = new();
    public List<CategoryStockDto> StockByCategory { get; set; } = new();
}

public class LowStockBookDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
}

public class CategoryStockDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int BookCount { get; set; }
    public int TotalStock { get; set; }
}

public class UserEngagementReportDto
{
    public int TotalRegisteredUsers { get; set; }
    public int ActiveUsersLast30Days { get; set; }
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<UserGrowthDto> UserGrowth { get; set; } = new();
}

public class TopCustomerDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

public class UserGrowthDto
{
    public string Month { get; set; } = string.Empty;
    public int NewUsers { get; set; }
}

public class ReviewAnalyticsReportDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<RatingCountDto> RatingDistribution { get; set; } = new();
    public List<TopRatedBookDto> TopRatedBooks { get; set; } = new();
}

public class RatingCountDto
{
    public int Rating { get; set; }
    public int Count { get; set; }
}

public class TopRatedBookDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
