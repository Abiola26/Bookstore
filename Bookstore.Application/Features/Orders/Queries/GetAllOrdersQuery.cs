using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Orders.Queries;

public record GetAllOrdersQuery(int PageNumber, int PageSize) : IRequest<ApiResponse<PagedResult<OrderResponseDto>>>;

public class GetAllOrdersHandler : IRequestHandler<GetAllOrdersQuery, ApiResponse<PagedResult<OrderResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllOrdersHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<OrderResponseDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1 || request.PageSize < 1)
            return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

        var orders = await _unitOfWork.Orders.GetAllOrdersPaginatedAsync(request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _unitOfWork.Orders.GetTotalOrderCountAsync(cancellationToken);

        var dtos = _mapper.Map<ICollection<OrderResponseDto>>(orders);
        var pagedResult = new PagedResult<OrderResponseDto>(dtos, totalCount, request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<OrderResponseDto>>.SuccessResponse(pagedResult);
    }
}
