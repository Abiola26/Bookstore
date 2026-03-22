using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Enum;

namespace Bookstore.Application.Features.Orders.Commands;

public record CancelOrderCommand(Guid OrderId) : IRequest<ApiResponse>;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async (ct) =>
            {
                var order = await _unitOfWork.Orders.GetWithItemsAsync(request.OrderId, ct);
                if (order == null)
                    throw new Exception("Order not found");

                if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                    throw new Exception("Cannot cancel completed or already cancelled orders");

                foreach (var item in order.Items)
                {
                    var book = await _unitOfWork.Books.GetByIdAsync(item.BookId, ct);
                    if (book != null)
                    {
                        book.TotalQuantity += item.Quantity;
                        _unitOfWork.Books.Update(book);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync(ct);

                return ApiResponse.SuccessResponse("Order cancelled successfully");
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse(ex.Message, null, 400);
        }
    }
}
