using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;
using Bookstore.Domain.ValueObjects;
using Bookstore.Domain.Enum;

namespace Bookstore.Application.Features.Orders.Commands;

public record CreateOrderCommand(Guid UserId, OrderCreateDto Dto, string? IdempotencyKey = null) : IRequest<ApiResponse<OrderResponseDto>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly Validators.OrderCreateDtoValidator _validator;

    public CreateOrderHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = new Validators.OrderCreateDtoValidator();
    }

    public async Task<ApiResponse<OrderResponseDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request.Dto);
        if (validationErrors.Count > 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        // IDEMPOTENCY CHECK
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await _unitOfWork.Orders.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingOrder != null)
            {
                if (existingOrder.UserId != request.UserId)
                    return ApiResponse<OrderResponseDto>.ErrorResponse("Invalid idempotency key for this user", null, 409);

                return ApiResponse<OrderResponseDto>.SuccessResponse(_mapper.Map<OrderResponseDto>(existingOrder), "Order already exists (idempotent result)");
            }
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<OrderResponseDto>.ErrorResponse("User not found", null, 404);

        if (!user.EmailConfirmed)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Email must be confirmed before place an order", null, 403);

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                var paymentMethod = Domain.Enum.PaymentMethod.CashOnDelivery;
                if (!string.IsNullOrEmpty(request.Dto.PaymentMethod))
                {
                    if (!System.Enum.TryParse<Domain.Enum.PaymentMethod>(request.Dto.PaymentMethod, true, out paymentMethod))
                    {
                        return ApiResponse<OrderResponseDto>.ErrorResponse($"Invalid payment method: {request.Dto.PaymentMethod}", null, 400);
                    }
                }

                var order = new Order(request.UserId)
                {
                    IdempotencyKey = request.IdempotencyKey,
                    ShippingAddress = request.Dto.ShippingAddress,
                    PaymentMethod = paymentMethod,
                    ShippingFee = new Money(5.00m, "USD")
                };

                foreach (var itemDto in request.Dto.Items)
                {
                    var book = await _unitOfWork.Books.GetByIdAsync(itemDto.BookId, ct);
                    if (book == null)
                        throw new Exception($"Book with ID {itemDto.BookId} not found");

                    if (order.Items.Count == 0)
                    {
                        // Initialize TotalAmount currency to match the first book's currency
                        order.TotalAmount = Money.Zero(book.Price.Currency);
                    }
                    else if (book.Price.Currency != order.TotalAmount.Currency)
                    {
                        throw new Exception("Mixed currencies in a single order are not allowed");
                    }

                    if (book.TotalQuantity < itemDto.Quantity)
                        throw new Exception($"Book '{book.Title}' is out of stock. Requested: {itemDto.Quantity}, Available: {book.TotalQuantity}");

                    var orderItem = new OrderItem(order.Id, itemDto.BookId, itemDto.Quantity, book.Price);
                    order.AddItem(orderItem);
                    await _unitOfWork.OrderItems.AddAsync(orderItem, ct);

                    book.TotalQuantity -= itemDto.Quantity;
                    _unitOfWork.Books.Update(book);
                }

                await _unitOfWork.Orders.AddAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var createdOrder = await _unitOfWork.Orders.GetWithItemsAsync(order.Id, ct);
                return ApiResponse<OrderResponseDto>.SuccessResponse(_mapper.Map<OrderResponseDto>(createdOrder), "Order created successfully", 201);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
            return ApiResponse<OrderResponseDto>.ErrorResponse(message, null, 400);
        }
    }
}
