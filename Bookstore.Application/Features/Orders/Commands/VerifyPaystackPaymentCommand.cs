using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Domain.Enum;

namespace Bookstore.Application.Features.Orders.Commands;

public record VerifyPaystackPaymentCommand(Guid OrderId, string Reference) : IRequest<ApiResponse<OrderResponseDto>>;

public class VerifyPaystackPaymentHandler : IRequestHandler<VerifyPaystackPaymentCommand, ApiResponse<OrderResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPaystackService _paystackService;

    public VerifyPaystackPaymentHandler(IUnitOfWork unitOfWork, IMapper mapper, IPaystackService paystackService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _paystackService = paystackService;
    }

    public async Task<ApiResponse<OrderResponseDto>> Handle(VerifyPaystackPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetWithItemsAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found", null, 404);

        if (order.IsPaid)
            return ApiResponse<OrderResponseDto>.SuccessResponse(_mapper.Map<OrderResponseDto>(order), "Order is already paid");

        var verification = await _paystackService.VerifyTransactionAsync(request.Reference);
        if (verification.Success)
        {
            order.IsPaid = true;
            order.PaymentReference = request.Reference;
            order.Status = OrderStatus.Paid;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<OrderResponseDto>.SuccessResponse(_mapper.Map<OrderResponseDto>(order), "Payment verified successfully");
        }

        return ApiResponse<OrderResponseDto>.ErrorResponse(verification.Message ?? "Payment verification failed", null, 400);
    }
}
