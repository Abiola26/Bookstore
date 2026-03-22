using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Reviews.Commands;

public record DeleteReviewCommand(Guid ReviewId, Guid UserId, bool IsAdmin) : IRequest<ApiResponse>;

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReviewHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review == null)
            return ApiResponse.ErrorResponse("Review not found", null, 404);

        if (!request.IsAdmin && review.UserId != request.UserId)
            return ApiResponse.ErrorResponse("You can only delete your own reviews", null, 403);

        var bookId = review.BookId;
        _unitOfWork.Reviews.Delete(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Sync book ratings
        await SyncBookRatingsAsync(bookId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Review deleted successfully");
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
