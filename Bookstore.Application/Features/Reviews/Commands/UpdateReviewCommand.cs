using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Reviews.Commands;

public record UpdateReviewCommand(Guid ReviewId, Guid UserId, ReviewUpdateDto Dto) : IRequest<ApiResponse<ReviewResponseDto>>;

public class UpdateReviewHandler : IRequestHandler<UpdateReviewCommand, ApiResponse<ReviewResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateReviewHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReviewResponseDto>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review == null)
            return ApiResponse<ReviewResponseDto>.ErrorResponse("Review not found", null, 404);

        if (review.UserId != request.UserId)
            return ApiResponse<ReviewResponseDto>.ErrorResponse("You can only update your own reviews", null, 403);

        bool ratingChanged = false;
        if (request.Dto.Rating.HasValue)
        {
            if (request.Dto.Rating < 1 || request.Dto.Rating > 5)
                return ApiResponse<ReviewResponseDto>.ErrorResponse("Rating must be between 1 and 5", null, 400);

            if (review.Rating != request.Dto.Rating.Value)
            {
                review.Rating = request.Dto.Rating.Value;
                ratingChanged = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Dto.Comment))
        {
            review.Comment = request.Dto.Comment;
        }

        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (ratingChanged)
        {
            await SyncBookRatingsAsync(review.BookId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        var responseDto = _mapper.Map<ReviewResponseDto>(review);
        responseDto.UserFullName = user?.FullName ?? "Unknown User";

        return ApiResponse<ReviewResponseDto>.SuccessResponse(responseDto);
    }

    private async Task SyncBookRatingsAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(bookId, cancellationToken);
        if (book != null)
        {
            var reviews = await _unitOfWork.Reviews.GetByBookIdAsync(bookId, cancellationToken);
            book.ReviewCount = reviews.Count;
            book.AverageRating = await _unitOfWork.Reviews.GetAverageRatingAsync(bookId, cancellationToken);
            _unitOfWork.Books.Update(book);
        }
    }
}
