using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.ShoppingCart.Commands;

public record RemoveFromCartCommand(Guid UserId, Guid CartItemId) : IRequest<ApiResponse<ShoppingCartResponseDto>>;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, ApiResponse<ShoppingCartResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RemoveFromCartHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(request.UserId, cancellationToken);
        if (cart == null)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Shopping cart not found", null, 404);

        var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == request.CartItemId);
        if (cartItem == null)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Item not found in cart", null, 404);

        cart.RemoveItem(request.CartItemId);
        _unitOfWork.ShoppingCarts.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _unitOfWork.ShoppingCarts.GetWithItemsAsync(cart.Id, cancellationToken);
        return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(_mapper.Map<ShoppingCartResponseDto>(updatedCart!));
    }
}
