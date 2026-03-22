using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Reports.Queries;

public record GetInventoryReportQuery() : IRequest<ApiResponse<InventoryReportDto>>;

public class GetInventoryReportHandler : IRequestHandler<GetInventoryReportQuery, ApiResponse<InventoryReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetInventoryReportHandler> _logger;

    public GetInventoryReportHandler(IUnitOfWork unitOfWork, ILogger<GetInventoryReportHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<InventoryReportDto>> Handle(GetInventoryReportQuery request, CancellationToken cancellationToken)
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
}
