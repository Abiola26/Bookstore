using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Microsoft.Extensions.Logging;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;

namespace Bookstore.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly OrderCreateDtoValidator _createValidator;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while retrieving the order", null, 500);
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
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
            return ApiResponse<ICollection<OrderResponseDto>>.ErrorResponse("An error occurred while retrieving orders", null, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<OrderResponseDto>>> GetUserOrdersPaginatedAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
                return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("User not found", null, 404);

            var orders = await _unitOfWork.Orders.GetByUserIdPaginatedAsync(userId, pageNumber, pageSize, cancellationToken);
            var totalCount = await _unitOfWork.Orders.GetUserOrderCountAsync(userId, cancellationToken);

            var dtos = orders.Select(MapToDto).ToList();
            var pagedResult = new PagedResult<OrderResponseDto>(dtos, totalCount, pageNumber, pageSize);

            return ApiResponse<PagedResult<OrderResponseDto>>.SuccessResponse(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged orders for user {UserId}", userId);
            return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("An error occurred while retrieving orders", null, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<OrderResponseDto>>> GetAllOrdersPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
                return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

            var orders = await _unitOfWork.Orders.GetAllOrdersPaginatedAsync(pageNumber, pageSize, cancellationToken);
            var totalCount = await _unitOfWork.Orders.GetTotalOrderCountAsync(cancellationToken);

            var dtos = orders.Select(MapToDto).ToList();
            var pagedResult = new PagedResult<OrderResponseDto>(dtos, totalCount, pageNumber, pageSize);

            return ApiResponse<PagedResult<OrderResponseDto>>.SuccessResponse(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all paged orders");
            return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("An error occurred while retrieving orders", null, 500);
        }
    }

    public async Task<ApiResponse<OrderResponseDto>> CreateOrderAsync(Guid userId, OrderCreateDto dto, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var validationErrors = _createValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        try
        {
            // IDEMPOTENCY CHECK
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existingOrder = await _unitOfWork.Orders.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
                if (existingOrder != null)
                {
                    // If order belongs to a different user, return error
                    if (existingOrder.UserId != userId)
                        return ApiResponse<OrderResponseDto>.ErrorResponse("Invalid idempotency key for this user", null, 409);

                    return ApiResponse<OrderResponseDto>.SuccessResponse(MapToDto(existingOrder), "Order already exists (idempotent result)");
                }
            }

            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse<OrderResponseDto>.ErrorResponse("User not found", null, 404);

            // SECURITY FIX: Enforce email confirmation for critical operations
            if (!user.EmailConfirmed)
                return ApiResponse<OrderResponseDto>.ErrorResponse("Email must be confirmed before place an order", null, 403);

        return await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
        {
            var order = new Order(userId)
            {
                IdempotencyKey = idempotencyKey
            };

            foreach (var itemDto in dto.Items)
            {
                var book = await _unitOfWork.Books.GetByIdAsync(itemDto.BookId, ct);
                if (book == null)
                {
                    throw new Bookstore.Application.Exceptions.NotFoundException($"Book with ID {itemDto.BookId} not found");
                }

                // Validate currency consistency (Orders must be single-currency)
                if (order.OrderItems.Count > 0 && book.Price.Currency != order.TotalAmount.Currency)
                {
                    throw new Bookstore.Application.Exceptions.BusinessException("Mixed currencies in a single order are not allowed");
                }

                // Check stock
                if (book.TotalQuantity < itemDto.Quantity)
                {
                    throw new Bookstore.Application.Exceptions.OutOfStockException(book.Title, itemDto.Quantity, book.TotalQuantity);
                }

                // Create and add order item (automatically updates order.TotalAmount)
                var orderItem = new OrderItem(order.Id, itemDto.BookId, itemDto.Quantity, book.Price);
                order.AddItem(orderItem);
                await _unitOfWork.OrderItems.AddAsync(orderItem, ct);

                // Reduce stock
                book.TotalQuantity -= itemDto.Quantity;
                _unitOfWork.Books.Update(book);
            }

            await _unitOfWork.Orders.AddAsync(order, ct);
            // SaveChangesAsync is called by the ExecuteInTransactionAsync wrapper
            
            // We need to return the DTO, but the outer wrapper expects ApiResponse<OrderResponseDto>
            // Actually, I should probably return the DTO and wrap it outside, or return the ApiResponse from within.
            // Returning the ApiResponse from within is easier given the return type is Task<T>.
            
            // Wait, I need to fetch with items for the DTO
            await _unitOfWork.SaveChangesAsync(ct); // Ensure saved so we can fetch back if needed, 
            // but the wrapper also calls it. Let's do it here to be safe and fetch.
            
            var createdOrder = await _unitOfWork.Orders.GetWithItemsAsync(order.Id, ct);
            return ApiResponse<OrderResponseDto>.SuccessResponse(
                MapToDto(createdOrder!),
                "Order created successfully",
                201);
        }, cancellationToken);
    }
    catch (Bookstore.Application.Exceptions.NotFoundException ex)
    {
        return ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message, null, 404);
    }
    catch (Bookstore.Application.Exceptions.BusinessException ex)
    {
        return ApiResponse<OrderResponseDto>.ErrorResponse(ex.Message, null, 400);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating order for user {UserId}", userId);
        return ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while creating the order", null, 500);
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
            _logger.LogError(ex, "Error updating status for order {OrderId}", orderId);
            return ApiResponse<OrderResponseDto>.ErrorResponse("An error occurred while updating order status", null, 500);
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
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return ApiResponse.ErrorResponse("An error occurred while cancelling the order", null, 500);
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
