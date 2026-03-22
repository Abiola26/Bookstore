using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<ApiResponse<OrderResponseDto>>;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, ApiResponse<OrderResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<OrderResponseDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetWithItemsAsync(request.Id, cancellationToken);
        if (order == null)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found", null, 404);

        return ApiResponse<OrderResponseDto>.SuccessResponse(_mapper.Map<OrderResponseDto>(order));
    }
}
