using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.ShoppingCart.Commands;

public record UpdateCartItemCommand(Guid UserId, Guid CartItemId, UpdateCartItemDto Dto) : IRequest<ApiResponse<ShoppingCartResponseDto>>;

public class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand, ApiResponse<ShoppingCartResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCartItemHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.Quantity <= 0)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Quantity must be greater than 0", null, 400);

        var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(request.UserId, cancellationToken);
        if (cart == null)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Shopping cart not found", null, 404);

        var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == request.CartItemId);
        if (cartItem == null)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Item not found in cart", null, 404);

        var book = await _unitOfWork.Books.GetByIdAsync(cartItem.BookId, cancellationToken);
        if (book == null || book.IsDeleted)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Book is no longer available", null, 410);

        if (book.TotalQuantity < request.Dto.Quantity)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Insufficient stock for this quantity", null, 400);

        cart.UpdateItemQuantity(request.CartItemId, request.Dto.Quantity);
        _unitOfWork.ShoppingCarts.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _unitOfWork.ShoppingCarts.GetWithItemsAsync(cart.Id, cancellationToken);
        return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(_mapper.Map<ShoppingCartResponseDto>(updatedCart!));
    }
}
