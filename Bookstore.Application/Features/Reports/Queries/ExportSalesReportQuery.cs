using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Reports.Queries;

public record ExportSalesReportQuery(DateTimeOffset StartDate, DateTimeOffset EndDate) : IRequest<ApiResponse<byte[]>>;

public class ExportSalesReportHandler : IRequestHandler<ExportSalesReportQuery, ApiResponse<byte[]>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportSalesReportHandler> _logger;

    public ExportSalesReportHandler(IUnitOfWork unitOfWork, ILogger<ExportSalesReportHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<byte[]>> Handle(ExportSalesReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _unitOfWork.Orders.GetAllAsync(cancellationToken);
            var filteredOrders = orders
                .Where(o => o.CreatedAt >= request.StartDate && o.CreatedAt <= request.EndDate)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);

            await writer.WriteLineAsync("OrderId,Date,Customer,Amount,Status");
            foreach (var order in filteredOrders)
            {
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
}
