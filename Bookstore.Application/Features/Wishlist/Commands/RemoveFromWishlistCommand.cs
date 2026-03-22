using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Wishlist.Commands;

public record RemoveFromWishlistCommand(Guid UserId, Guid BookId) : IRequest<ApiResponse>;

public class RemoveFromWishlistHandler : IRequestHandler<RemoveFromWishlistCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromWishlistHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var wishlistItem = await _unitOfWork.Wishlist.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);
        if (wishlistItem == null)
            return ApiResponse.ErrorResponse("Book is not in wishlist", null, 404);

        _unitOfWork.Wishlist.Delete(wishlistItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Book removed from wishlist successfully");
    }
}
