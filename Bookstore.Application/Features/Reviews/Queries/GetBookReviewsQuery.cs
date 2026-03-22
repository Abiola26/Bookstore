using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Reviews.Queries;

public record GetBookReviewsQuery(Guid BookId) : IRequest<ApiResponse<ICollection<ReviewResponseDto>>>;

public class GetBookReviewsHandler : IRequestHandler<GetBookReviewsQuery, ApiResponse<ICollection<ReviewResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookReviewsHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ICollection<ReviewResponseDto>>> Handle(GetBookReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _unitOfWork.Reviews.GetByBookIdAsync(request.BookId, cancellationToken);
        var dtos = _mapper.Map<ICollection<ReviewResponseDto>>(reviews);
        return ApiResponse<ICollection<ReviewResponseDto>>.SuccessResponse(dtos);
    }
}
