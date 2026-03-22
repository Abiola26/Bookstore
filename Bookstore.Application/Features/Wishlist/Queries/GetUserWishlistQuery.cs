using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Wishlist.Queries;

public record GetUserWishlistQuery(Guid UserId) : IRequest<ApiResponse<ICollection<BookResponseDto>>>;

public class GetUserWishlistHandler : IRequestHandler<GetUserWishlistQuery, ApiResponse<ICollection<BookResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserWishlistHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ICollection<BookResponseDto>>> Handle(GetUserWishlistQuery request, CancellationToken cancellationToken)
    {
        var wishlistItems = await _unitOfWork.Wishlist.GetByUserIdAsync(request.UserId, cancellationToken);
        var books = wishlistItems.Select(w => w.Book).ToList();
        var dtos = _mapper.Map<ICollection<BookResponseDto>>(books);
        return ApiResponse<ICollection<BookResponseDto>>.SuccessResponse(dtos);
    }
}
