using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Reports.Queries;

public record GetDashboardReportQuery() : IRequest<ApiResponse<DashboardReportDto>>;

public class GetDashboardReportHandler : IRequestHandler<GetDashboardReportQuery, ApiResponse<DashboardReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetDashboardReportHandler> _logger;

    public GetDashboardReportHandler(IUnitOfWork unitOfWork, ILogger<GetDashboardReportHandler> _logger)
    {
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<ApiResponse<DashboardReportDto>> Handle(GetDashboardReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var totalOrders = await _unitOfWork.Orders.GetTotalOrderCountAsync(cancellationToken);
            var totalBooks = await _unitOfWork.Books.GetTotalCountAsync(cancellationToken);
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var totalUsers = users.Count;
            var totalRevenue = await _unitOfWork.Reports.GetTotalRevenueAsync(cancellationToken);

            var topBooks = await _unitOfWork.Reports.GetTopSellingBooksAsync(5, cancellationToken);
            var monthlySales = await _unitOfWork.Reports.GetMonthlySalesAsync(6, cancellationToken);
            var salesByCategory = await _unitOfWork.Reports.GetSalesByCategoryAsync(cancellationToken);

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
}
