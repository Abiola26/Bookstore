using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Books.Queries;

public record GetBooksPagedQuery(int PageNumber, int PageSize) : IRequest<ApiResponse<PagedResult<BookResponseDto>>>;

public class GetBooksPagedHandler : IRequestHandler<GetBooksPagedQuery, ApiResponse<PagedResult<BookResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBooksPagedHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<BookResponseDto>>> Handle(GetBooksPagedQuery request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1 || request.PageSize < 1)
            return ApiResponse<PagedResult<BookResponseDto>>.ErrorResponse("Page number and page size must be greater than 0", null, 400);

        var books = await _unitOfWork.Books.GetPaginatedAsync(request.PageNumber, request.PageSize, cancellationToken);
        var totalCount = await _unitOfWork.Books.GetTotalCountAsync(cancellationToken);

        var dtos = _mapper.Map<ICollection<BookResponseDto>>(books);
        var pagedResult = new PagedResult<BookResponseDto>(dtos, totalCount, request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<BookResponseDto>>.SuccessResponse(pagedResult);
    }
}
