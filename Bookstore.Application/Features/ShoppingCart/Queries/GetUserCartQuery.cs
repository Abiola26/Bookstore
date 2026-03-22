using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;

namespace Bookstore.Application.Features.ShoppingCart.Queries;

public record GetUserCartQuery(Guid UserId) : IRequest<ApiResponse<ShoppingCartResponseDto>>;

public class GetUserCartHandler : IRequestHandler<GetUserCartQuery, ApiResponse<ShoppingCartResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserCartHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> Handle(GetUserCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = new Domain.Entities.ShoppingCart(request.UserId);
            await _unitOfWork.ShoppingCarts.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(_mapper.Map<ShoppingCartResponseDto>(cart));
    }
}
