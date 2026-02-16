using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Bookstore.Application.Exceptions;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;

namespace Bookstore.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderCreateDtoValidator _createValidator;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _createValidator = new OrderCreateDtoValidator();
    }

    public async Task<ApiResponse<OrderResponseDto>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _unitOfWork.Orders.GetWithItemsAsync(id, cancellationToken);
            if (order == null)
                return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found", null, 404);

            return ApiResponse<OrderResponseDto>.SuccessResponse(MapToDto(order));
        }
        catch (Exception ex)
        {
            return ApiResponse<OrderResponseDto>.ErrorResponse($"Failed to retrieve order: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<OrderResponseDto>>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse<ICollection<OrderResponseDto>>.ErrorResponse("User not found", null, 404);

            var orders = await _unitOfWork.Orders.GetByUserIdAsync(userId, cancellationToken);
            var dtos = orders.Select(MapToDto).ToList();

            return ApiResponse<ICollection<OrderResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<ICollection<OrderResponseDto>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<ICollection<OrderResponseDto>>> GetUserOrdersPaginatedAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
                return ApiResponse<ICollection<OrderResponseDto>>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse<ICollection<OrderResponseDto>>.ErrorResponse("User not found", null, 404);

            var orders = await _unitOfWork.Orders.GetByUserIdPaginatedAsync(userId, pageNumber, pageSize, cancellationToken);
            var dtos = orders.Select(MapToDto).ToList();

            return ApiResponse<ICollection<OrderResponseDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<ICollection<OrderResponseDto>>.ErrorResponse($"Failed to retrieve orders: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(Guid userId, OrderCreateDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = _createValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        try
        {
            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse<OrderResponseDto>.ErrorResponse("User not found", null, 404);

            // Start transaction for order creation and inventory update
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = new Order(userId);
                var totalAmount = Money.Zero("USD");

                foreach (var itemDto in dto.Items)
                {
                    var book = await _unitOfWork.Books.GetByIdAsync(itemDto.BookId, cancellationToken);
                    if (book == null)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return ApiResponse<OrderResponseDto>.ErrorResponse(
                            $"Book with ID {itemDto.BookId} not found",
                            null, 404);
                    }

                    // Check stock
                    if (book.TotalQuantity < itemDto.Quantity)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return ApiResponse<OrderResponseDto>.ErrorResponse(
                            $"Insufficient stock for book '{book.Title}'",
                            new List<string> { $"Requested: {itemDto.Quantity}, Available: {book.TotalQuantity}" },
                            400);
                    }

                    // Create order item
                    var orderItem = new OrderItem(order.Id, itemDto.BookId, itemDto.Quantity, book.Price);
                    order.AddItem(orderItem);
                    await _unitOfWork.OrderItems.AddAsync(orderItem, cancellationToken);

                    // Reduce stock
                    book.TotalQuantity -= itemDto.Quantity;
                    _unitOfWork.Books.Update(book);

                    totalAmount = totalAmount + (book.Price * itemDto.Quantity);
                }

                // Ensure order has correct total
                order.TotalAmount = totalAmount;

                await _unitOfWork.Orders.AddAsync(order, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Fetch full order details
                var createdOrder = await _unitOfWork.Orders.GetWithItemsAsync(order.Id, cancellationToken);
                return ApiResponse<OrderResponseDto>.SuccessResponse(
                    MapToDto(createdOrder!),
                    "Order created successfully",
                    201);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<OrderResponseDto>.ErrorResponse($"Failed to create order: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<OrderResponseDto>> UpdateOrderStatusAsync(Guid orderId, OrderUpdateStatusDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Status))
                return ApiResponse<OrderResponseDto>.ErrorResponse("Status is required", null, 400);

            if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
                return ApiResponse<OrderResponseDto>.ErrorResponse(
                    "Invalid status. Valid values: Pending, Paid, Cancelled, Shipped, Completed",
                    null, 400);

            var order = await _unitOfWork.Orders.GetWithItemsAsync(orderId, cancellationToken);
            if (order == null)
                return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found", null, 404);

            // Validate status transitions
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                return ApiResponse<OrderResponseDto>.ErrorResponse(
                    "Cannot update status of completed or cancelled orders",
                    null, 400);

            order.Status = newStatus;
            order.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<OrderResponseDto>.SuccessResponse(MapToDto(order), "Order status updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<OrderResponseDto>.ErrorResponse($"Failed to update order status: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _unitOfWork.Orders.GetWithItemsAsync(orderId, cancellationToken);
                if (order == null)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return ApiResponse.ErrorResponse("Order not found", null, 404);
                }

                if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return ApiResponse.ErrorResponse("Cannot cancel completed or already cancelled orders", null, 400);
                }

                // Restore stock for all items
                foreach (var item in order.OrderItems)
                {
                    var book = await _unitOfWork.Books.GetByIdAsync(item.BookId, cancellationToken);
                    if (book != null)
                    {
                        book.TotalQuantity += item.Quantity;
                        _unitOfWork.Books.Update(book);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                _unitOfWork.Orders.Update(order);

                await _unitOfWork.CommitAsync(cancellationToken);
                return ApiResponse.SuccessResponse("Order cancelled successfully");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Failed to cancel order: {ex.Message}", null, 500);
        }
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserFullName = order.User?.FullName ?? string.Empty,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            Status = order.Status.ToString(),
            Items = order.OrderItems.Select(oi => new OrderItemResponseDto
            {
                Id = oi.Id,
                BookId = oi.BookId,
                BookTitle = oi.Book?.Title ?? string.Empty,
                ISBN = oi.Book?.ISBN.ToString() ?? string.Empty,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice.Amount,
                Currency = oi.UnitPrice.Currency
            }).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
