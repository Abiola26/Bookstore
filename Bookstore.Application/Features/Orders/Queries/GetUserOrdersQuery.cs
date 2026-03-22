using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Orders.Queries;

public record GetUserOrdersQuery(Guid UserId, int PageNumber, int PageSize) : IRequest<ApiResponse<PagedResult<OrderResponseDto>>>;

public class GetUserOrdersHandler : IRequestHandler<GetUserOrdersQuery, ApiResponse<PagedResult<OrderResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserOrdersHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<OrderResponseDto>>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1 || request.PageSize < 1)
            return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<PagedResult<OrderResponseDto>>.ErrorResponse("User not found", null, 404);

        var orders = await _unitOfWork.Orders.GetByUserIdPaginatedAsync(request.UserId, request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _unitOfWork.Orders.GetUserOrderCountAsync(request.UserId, cancellationToken);

        var dtos = _mapper.Map<ICollection<OrderResponseDto>>(orders);
        var pagedResult = new PagedResult<OrderResponseDto>(dtos, totalCount, request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<OrderResponseDto>>.SuccessResponse(pagedResult);
    }
}
