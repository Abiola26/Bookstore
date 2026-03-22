using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Domain.Entities;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Reviews.Commands;

public record AddReviewCommand(Guid BookId, Guid UserId, ReviewCreateDto Dto) : IRequest<ApiResponse<ReviewResponseDto>>;

public class AddReviewHandler : IRequestHandler<AddReviewCommand, ApiResponse<ReviewResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddReviewHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReviewResponseDto>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
            return ApiResponse<ReviewResponseDto>.ErrorResponse("Book not found", null, 404);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<ReviewResponseDto>.ErrorResponse("User not found", null, 404);

        if (!user.EmailConfirmed)
            return ApiResponse<ReviewResponseDto>.ErrorResponse("Email must be confirmed before posting reviews", null, 403);

        if (await _unitOfWork.Reviews.HasUserReviewedBookAsync(request.UserId, request.BookId, cancellationToken))
            return ApiResponse<ReviewResponseDto>.ErrorResponse("You have already reviewed this book", null, 400);

        var review = new Review(request.BookId, request.UserId, request.Dto.Rating, request.Dto.Comment);

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Sync book ratings
        await SyncBookRatingsAsync(request.BookId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var responseDto = _mapper.Map<ReviewResponseDto>(review);
        responseDto.UserFullName = user.FullName; // Mapping profile might handle this if User is included

        return ApiResponse<ReviewResponseDto>.SuccessResponse(responseDto, "Review added successfully", 201);
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
