using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Categories.Queries;

public record GetAllCategoriesQuery : IRequest<ApiResponse<ICollection<CategoryResponseDto>>>;

public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, ApiResponse<ICollection<CategoryResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllCategoriesHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ICollection<CategoryResponseDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<ICollection<CategoryResponseDto>>(categories);
        return ApiResponse<ICollection<CategoryResponseDto>>.SuccessResponse(dtos);
    }
}
