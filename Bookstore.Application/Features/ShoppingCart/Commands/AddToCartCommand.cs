using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.ShoppingCart.Commands;

public record AddToCartCommand(Guid UserId, AddToCartDto Dto) : IRequest<ApiResponse<ShoppingCartResponseDto>>;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, ApiResponse<ShoppingCartResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddToCartHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.Quantity <= 0)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Quantity must be greater than 0", null, 400);

        var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(request.UserId, cancellationToken);
        if (cart == null)
        {
            cart = new Domain.Entities.ShoppingCart(request.UserId);
            await _unitOfWork.ShoppingCarts.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var book = await _unitOfWork.Books.GetByIdAsync(request.Dto.BookId, cancellationToken);
        if (book == null)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Book not found", null, 404);

        if (book.IsDeleted)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("This book is no longer available", null, 410);

        if (book.TotalQuantity < request.Dto.Quantity)
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Insufficient stock for this book", null, 400);

        var unitPrice = new Money(book.Price.Amount, book.Price.Currency);
        var cartItem = new ShoppingCartItem(cart.Id, request.Dto.BookId, request.Dto.Quantity, unitPrice);
        cart.AddItem(cartItem);

        _unitOfWork.ShoppingCarts.Update(cart);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _unitOfWork.ShoppingCarts.GetWithItemsAsync(cart.Id, cancellationToken);
        return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(_mapper.Map<ShoppingCartResponseDto>(updatedCart!));
    }
}
