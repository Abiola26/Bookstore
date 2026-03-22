using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Reports.Queries;

public record GetUserEngagementReportQuery() : IRequest<ApiResponse<UserEngagementReportDto>>;

public class GetUserEngagementReportHandler : IRequestHandler<GetUserEngagementReportQuery, ApiResponse<UserEngagementReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserEngagementReportHandler> _logger;

    public GetUserEngagementReportHandler(IUnitOfWork unitOfWork, ILogger<GetUserEngagementReportHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UserEngagementReportDto>> Handle(GetUserEngagementReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var totalUsers = users.Count;
            var activeThreshold = DateTimeOffset.UtcNow.AddDays(-30);
            var orders = await _unitOfWork.Orders.GetAllAsync(cancellationToken);
            var activeUsersCount = orders
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
}
