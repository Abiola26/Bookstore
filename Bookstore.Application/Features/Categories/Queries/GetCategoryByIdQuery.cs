using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Categories.Queries;

public record GetCategoryByIdQuery(Guid Id) : IRequest<ApiResponse<CategoryResponseDto>>;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, ApiResponse<CategoryResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CategoryResponseDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            return ApiResponse<CategoryResponseDto>.ErrorResponse("Category not found", null, 404);

        return ApiResponse<CategoryResponseDto>.SuccessResponse(_mapper.Map<CategoryResponseDto>(category));
    }
}
