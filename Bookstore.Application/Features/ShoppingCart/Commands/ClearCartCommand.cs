using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.ShoppingCart.Commands;

public record ClearCartCommand(Guid UserId) : IRequest<ApiResponse<ShoppingCartResponseDto>>;

public class ClearCartHandler : IRequestHandler<ClearCartCommand, ApiResponse<ShoppingCartResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ClearCartHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(request.UserId, cancellationToken);
        if (cart == null)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Shopping cart not found", null, 404);

        cart.Clear();
        _unitOfWork.ShoppingCarts.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(_mapper.Map<ShoppingCartResponseDto>(cart));
    }
}
