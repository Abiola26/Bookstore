using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Enum;

namespace Bookstore.Application.Features.Orders.Commands;

public record UpdateOrderStatusCommand(Guid OrderId, OrderUpdateStatusDto Dto) : IRequest<ApiResponse<OrderResponseDto>>;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResponse<OrderResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateOrderStatusHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<OrderResponseDto>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Dto.Status))
            return ApiResponse<OrderResponseDto>.ErrorResponse("Status is required", null, 400);

        if (!Enum.TryParse<OrderStatus>(request.Dto.Status, true, out var newStatus))
            return ApiResponse<OrderResponseDto>.ErrorResponse("Invalid status", null, 400);

        var order = await _unitOfWork.Orders.GetWithItemsAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found", null, 404);

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Cannot update completed or cancelled orders", null, 400);

        order.Status = newStatus;
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<OrderResponseDto>.SuccessResponse(_mapper.Map<OrderResponseDto>(order), "Order status updated successfully");
    }
}
