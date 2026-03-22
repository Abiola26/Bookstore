using Bookstore.Application.Common;
using Bookstore.Domain.Entities;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Wishlist.Commands;

public record AddToWishlistCommand(Guid UserId, Guid BookId) : IRequest<ApiResponse>;

public class AddToWishlistHandler : IRequestHandler<AddToWishlistCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddToWishlistHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Wishlist.ExistsAsync(request.UserId, request.BookId, cancellationToken);
        if (exists)
            return ApiResponse.ErrorResponse("Book is already in wishlist", null, 409);

        var book = await _unitOfWork.Books.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
            return ApiResponse.ErrorResponse("Book not found", null, 404);

        var wishlistItem = new WishlistItem(request.UserId, request.BookId);
        await _unitOfWork.Wishlist.AddAsync(wishlistItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Book added to wishlist successfully");
    }
}
