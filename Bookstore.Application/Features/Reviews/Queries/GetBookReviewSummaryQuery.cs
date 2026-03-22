using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Reviews.Queries;

public record GetBookReviewSummaryQuery(Guid BookId) : IRequest<ApiResponse<BookReviewSummaryDto>>;

public class GetBookReviewSummaryHandler : IRequestHandler<GetBookReviewSummaryQuery, ApiResponse<BookReviewSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookReviewSummaryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<BookReviewSummaryDto>> Handle(GetBookReviewSummaryQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _unitOfWork.Reviews.GetByBookIdAsync(request.BookId, cancellationToken);
        var averageRating = await _unitOfWork.Reviews.GetAverageRatingAsync(request.BookId, cancellationToken);

        var summary = new BookReviewSummaryDto
        {
            AverageRating = averageRating,
            ReviewCount = reviews.Count,
            RecentReviews = _mapper.Map<List<ReviewResponseDto>>(reviews.Take(5))
        };

        return ApiResponse<BookReviewSummaryDto>.SuccessResponse(summary);
    }
}
