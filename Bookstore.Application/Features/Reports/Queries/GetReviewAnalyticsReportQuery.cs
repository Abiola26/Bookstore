using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Reports.Queries;

public record GetReviewAnalyticsReportQuery() : IRequest<ApiResponse<ReviewAnalyticsReportDto>>;

public class GetReviewAnalyticsReportHandler : IRequestHandler<GetReviewAnalyticsReportQuery, ApiResponse<ReviewAnalyticsReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetReviewAnalyticsReportHandler> _logger;

    public GetReviewAnalyticsReportHandler(IUnitOfWork unitOfWork, ILogger<GetReviewAnalyticsReportHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ReviewAnalyticsReportDto>> Handle(GetReviewAnalyticsReportQuery request, CancellationToken cancellationToken)
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
