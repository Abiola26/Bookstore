using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Common;
using Bookstore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Bookstore.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ReviewResponseDto>> AddReviewAsync(Guid bookId, Guid userId, ReviewCreateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _unitOfWork.Books.GetByIdAsync(bookId, cancellationToken);
            if (book == null)
                return ApiResponse<ReviewResponseDto>.ErrorResponse("Book not found", null, 404);

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse<ReviewResponseDto>.ErrorResponse("User not found", null, 404);

            // Check if user already reviewed this book
            if (await _unitOfWork.Reviews.HasUserReviewedBookAsync(userId, bookId, cancellationToken))
                return ApiResponse<ReviewResponseDto>.ErrorResponse("You have already reviewed this book", null, 400);

            var review = new Review(bookId, userId, dto.Rating, dto.Comment);

            await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Sync book ratings
            await SyncBookRatingsAsync(bookId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var responseDto = MapToResponseDto(review, user.FullName);
            return ApiResponse<ReviewResponseDto>.SuccessResponse(responseDto, "Review added successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding review for book {BookId} by user {UserId}", bookId, userId);
            return ApiResponse<ReviewResponseDto>.ErrorResponse("An error occurred while adding the review", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<ReviewResponseDto>>> GetBookReviewsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reviews = await _unitOfWork.Reviews.GetByBookIdAsync(bookId, cancellationToken);
            var dtos = reviews.Select(r => MapToResponseDto(r, r.User?.FullName ?? "Unknown User")).ToList();
            return ApiResponse<ICollection<ReviewResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for book {BookId}", bookId);
            return ApiResponse<ICollection<ReviewResponseDto>>.ErrorResponse("An error occurred while retrieving reviews", null, 500);
        }
    }

    public async Task<ApiResponse<BookReviewSummaryDto>> GetBookReviewSummaryAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reviews = await _unitOfWork.Reviews.GetByBookIdAsync(bookId, cancellationToken);
            var averageRating = await _unitOfWork.Reviews.GetAverageRatingAsync(bookId, cancellationToken);

            var summary = new BookReviewSummaryDto
            {
                AverageRating = averageRating,
                ReviewCount = reviews.Count,
                RecentReviews = reviews.Take(5).Select(r => MapToResponseDto(r, r.User?.FullName ?? "Unknown User")).ToList()
            };

            return ApiResponse<BookReviewSummaryDto>.SuccessResponse(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review summary for book {BookId}", bookId);
            return ApiResponse<BookReviewSummaryDto>.ErrorResponse("An error occurred while retrieving review summary", null, 500);
        }
    }

    public async Task<ApiResponse<ReviewResponseDto>> UpdateReviewAsync(Guid reviewId, Guid userId, ReviewUpdateDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);
            if (review == null)
                return ApiResponse<ReviewResponseDto>.ErrorResponse("Review not found", null, 404);

            if (review.UserId != userId)
                return ApiResponse<ReviewResponseDto>.ErrorResponse("You can only update your own reviews", null, 403);

            bool ratingChanged = false;
            if (dto.Rating.HasValue)
            {
                if (dto.Rating < 1 || dto.Rating > 5)
                    return ApiResponse<ReviewResponseDto>.ErrorResponse("Rating must be between 1 and 5", null, 400);
                
                if (review.Rating != dto.Rating.Value)
                {
                    review.Rating = dto.Rating.Value;
                    ratingChanged = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                review.Comment = dto.Comment;
            }

            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (ratingChanged)
            {
                await SyncBookRatingsAsync(review.BookId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            return ApiResponse<ReviewResponseDto>.SuccessResponse(MapToResponseDto(review, user?.FullName ?? "Unknown User"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId} by user {UserId}", reviewId, userId);
            return ApiResponse<ReviewResponseDto>.ErrorResponse("An error occurred while updating the review", null, 500);
        }
    }

    public async Task<ApiResponse> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId, cancellationToken);
            if (review == null)
                return ApiResponse.ErrorResponse("Review not found", null, 404);

            if (!isAdmin && review.UserId != userId)
                return ApiResponse.ErrorResponse("You can only delete your own reviews", null, 403);

            var bookId = review.BookId;
            _unitOfWork.Reviews.Delete(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Sync book ratings
            await SyncBookRatingsAsync(bookId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Review deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return ApiResponse.ErrorResponse("An error occurred while deleting the review", null, 500);
        }
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

    private ReviewResponseDto MapToResponseDto(Review review, string userFullName)
    {
        return new ReviewResponseDto
        {
            Id = review.Id,
            BookId = review.BookId,
            UserId = review.UserId,
            UserFullName = userFullName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}
