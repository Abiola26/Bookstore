using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Orders.Commands;

public record DeleteOrderCommand(Guid OrderId) : IRequest<ApiResponse>;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ApiResponse.ErrorResponse("Order not found", null, 404);

        _unitOfWork.Orders.Delete(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Order deleted successfully");
    }
}
