using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;

namespace Bookstore.Application.Features.Categories.Commands;

public record CreateCategoryCommand(CategoryCreateDto Dto) : IRequest<ApiResponse<CategoryResponseDto>>;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, ApiResponse<CategoryResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCategoryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CategoryResponseDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = _mapper.Map<Category>(request.Dto);
        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CategoryResponseDto>.SuccessResponse(_mapper.Map<CategoryResponseDto>(category), "Category created successfully", 201);
    }
}
