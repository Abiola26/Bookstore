using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;

namespace Bookstore.Application.Features.Orders.Queries;

public record GetOrderConfigurationQuery() : IRequest<ApiResponse<OrderConfigurationResponseDto>>;

public class GetOrderConfigurationHandler : IRequestHandler<GetOrderConfigurationQuery, ApiResponse<OrderConfigurationResponseDto>>
{
    public Task<ApiResponse<OrderConfigurationResponseDto>> Handle(GetOrderConfigurationQuery request, CancellationToken cancellationToken)
    {
        // For now, this is a flat fee, but in the future it could be fetched from database or settings
        var config = new OrderConfigurationResponseDto
        {
            ShippingFee = 5.00m,
            Currency = "USD"
        };

        return Task.FromResult(ApiResponse<OrderConfigurationResponseDto>.SuccessResponse(config));
    }
}
