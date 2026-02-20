using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Bookstore.Infrastructure.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(IUnitOfWork unitOfWork, ILogger<WishlistService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse> AddToWishlistAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Wishlist.ExistsAsync(userId, bookId, cancellationToken);
            if (exists)
                return ApiResponse.ErrorResponse("Book is already in wishlist", null, 409);

            var book = await _unitOfWork.Books.GetByIdAsync(bookId, cancellationToken);
            if (book == null)
                return ApiResponse.ErrorResponse("Book not found", null, 404);

            var wishlistItem = new WishlistItem(userId, bookId);
            await _unitOfWork.Wishlist.AddAsync(wishlistItem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Book added to wishlist successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding book {BookId} to wishlist for user {UserId}", bookId, userId);
            return ApiResponse.ErrorResponse("An error occurred while adding book to wishlist", null, 500);
        }
    }

    public async Task<ApiResponse> RemoveFromWishlistAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wishlistItem = await _unitOfWork.Wishlist.GetByUserAndBookAsync(userId, bookId, cancellationToken);
            if (wishlistItem == null)
                return ApiResponse.ErrorResponse("Book is not in wishlist", null, 404);

            _unitOfWork.Wishlist.Delete(wishlistItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Book removed from wishlist successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing book {BookId} from wishlist for user {UserId}", bookId, userId);
            return ApiResponse.ErrorResponse("An error occurred while removing book from wishlist", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<BookResponseDto>>> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wishlistItems = await _unitOfWork.Wishlist.GetByUserIdAsync(userId, cancellationToken);
            var dtos = wishlistItems.Select(w => MapToBookResponseDto(w.Book)).ToList();
            return ApiResponse<ICollection<BookResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist for user {UserId}", userId);
            return ApiResponse<ICollection<BookResponseDto>>.ErrorResponse("An error occurred while retrieving wishlist", null, 500);
        }
    }

    public async Task<ApiResponse<bool>> IsInWishlistAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _unitOfWork.Wishlist.ExistsAsync(userId, bookId, cancellationToken);
            return ApiResponse<bool>.SuccessResponse(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wishlist status for book {BookId} and user {UserId}", bookId, userId);
            return ApiResponse<bool>.ErrorResponse("An error occurred", null, 500);
        }
    }

    private BookResponseDto MapToBookResponseDto(Book book)
    {
        return new BookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN.Value,
            Price = book.Price.Amount,
            Currency = book.Price.Currency,
            Description = book.Description,
            Publisher = book.Publisher,
            PublicationDate = book.PublicationDate,
            Pages = book.Pages,
            Language = book.Language,
            CoverImageUrl = book.CoverImageUrl,
            TotalQuantity = book.TotalQuantity,
            CategoryId = book.CategoryId,
            CategoryName = book.Category?.Name ?? "Unknown",
            AverageRating = book.AverageRating,
            ReviewCount = book.ReviewCount,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };
    }
}
