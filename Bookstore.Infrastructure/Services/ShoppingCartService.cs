using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Bookstore.Infrastructure.Services;

public class ShoppingCartService : IShoppingCartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShoppingCartService> _logger;

    public ShoppingCartService(IUnitOfWork unitOfWork, ILogger<ShoppingCartService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> GetUserCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(userId, cancellationToken);
            
            if (cart == null)
            {
                // Create a new cart if it doesn't exist
                cart = new ShoppingCart(userId);
                await _unitOfWork.ShoppingCarts.AddAsync(cart, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(MapToDto(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("An error occurred while retrieving the cart", null, 500);
        }
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> AddToCartAsync(Guid userId, AddToCartDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            if (dto.Quantity <= 0)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Quantity must be greater than 0", null, 400);

            // Get or create cart
            var cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId, cancellationToken);
            if (cart == null)
            {
                cart = new ShoppingCart(userId);
                await _unitOfWork.ShoppingCarts.AddAsync(cart, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Get book
            var book = await _unitOfWork.Books.GetByIdAsync(dto.BookId, cancellationToken);
            if (book == null)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Book not found", null, 404);

            // Check if book is deleted
            if (book.IsDeleted)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("This book is no longer available", null, 410);

            // Check stock
            if (book.TotalQuantity < dto.Quantity)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Insufficient stock for this book", null, 400);

            // Create cart item with a new Money instance to avoid EF Core tracking issues
            var unitPrice = new Money(book.Price.Amount, book.Price.Currency);
            var cartItem = new ShoppingCartItem(cart.Id, dto.BookId, dto.Quantity, unitPrice);
            cart.AddItem(cartItem);

            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload cart with items
            cart = await _unitOfWork.ShoppingCarts.GetWithItemsAsync(cart.Id, cancellationToken);
            return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(MapToDto(cart!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for user {UserId}", userId);
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("An error occurred while adding item to cart", null, 500);
        }
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            if (dto.Quantity <= 0)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Quantity must be greater than 0", null, 400);

            // Get cart
            var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(userId, cancellationToken);
            if (cart == null)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Shopping cart not found", null, 404);

            // Find the cart item
            var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem == null)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Item not found in cart", null, 404);

            // Check stock availability
            var book = await _unitOfWork.Books.GetByIdAsync(cartItem.BookId, cancellationToken);
            if (book == null || book.IsDeleted)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Book is no longer available", null, 410);

            if (book.TotalQuantity < dto.Quantity)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Insufficient stock for this quantity", null, 400);

            // Update quantity
            cart.UpdateItemQuantity(cartItemId, dto.Quantity);
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload cart with items
            cart = await _unitOfWork.ShoppingCarts.GetWithItemsAsync(cart.Id, cancellationToken);
            return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(MapToDto(cart!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item {CartItemId} for user {UserId}", cartItemId, userId);
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("An error occurred while updating cart item", null, 500);
        }
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> RemoveFromCartAsync(Guid userId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get cart
            var cart = await _unitOfWork.ShoppingCarts.GetUserCartWithItemsAsync(userId, cancellationToken);
            if (cart == null)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Shopping cart not found", null, 404);

            // Check if item exists in cart
            var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
            if (cartItem == null)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Item not found in cart", null, 404);

            // Remove item
            cart.RemoveItem(cartItemId);
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload cart with items
            cart = await _unitOfWork.ShoppingCarts.GetWithItemsAsync(cart.Id, cancellationToken);
            return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(MapToDto(cart!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {CartItemId} from cart for user {UserId}", cartItemId, userId);
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("An error occurred while removing item from cart", null, 500);
        }
    }

    public async Task<ApiResponse<ShoppingCartResponseDto>> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get cart
            var cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId, cancellationToken);
            if (cart == null)
                return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("Shopping cart not found", null, 404);

            // Clear cart
            cart.Clear();
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<ShoppingCartResponseDto>.SuccessResponse(MapToDto(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
            return ApiResponse<ShoppingCartResponseDto>.ErrorResponse("An error occurred while clearing cart", null, 500);
        }
    }

    private static ShoppingCartResponseDto MapToDto(ShoppingCart cart)
    {
        return new ShoppingCartResponseDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            TotalPrice = cart.TotalPrice.Amount,
            Currency = cart.TotalPrice.Currency,
            ItemCount = cart.GetItemCount(),
            IsEmpty = cart.IsEmpty,
            Items = cart.Items.Select(item => new ShoppingCartItemResponseDto
            {
                Id = item.Id,
                BookId = item.BookId,
                BookTitle = item.Book?.Title ?? string.Empty,
                ISBN = item.Book?.ISBN.Value ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice.Amount,
                Currency = item.UnitPrice.Currency
            }).ToList(),
            LastModified = cart.LastModified
        };
    }
}
