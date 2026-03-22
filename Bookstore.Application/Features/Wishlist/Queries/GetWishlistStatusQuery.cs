using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Wishlist.Queries;

public record GetWishlistStatusQuery(Guid UserId, Guid BookId) : IRequest<ApiResponse<bool>>;

public class GetWishlistStatusHandler : IRequestHandler<GetWishlistStatusQuery, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWishlistStatusHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(GetWishlistStatusQuery request, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Wishlist.ExistsAsync(request.UserId, request.BookId, cancellationToken);
        return ApiResponse<bool>.SuccessResponse(exists);
    }
}
