using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Categories.Commands;

public record UpdateCategoryCommand(Guid Id, CategoryUpdateDto Dto) : IRequest<ApiResponse<CategoryResponseDto>>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, ApiResponse<CategoryResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCategoryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<CategoryResponseDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            return ApiResponse<CategoryResponseDto>.ErrorResponse("Category not found", null, 404);

        if (request.Dto.Name != null)
        {
            category.Name = request.Dto.Name;
        }
        category.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CategoryResponseDto>.SuccessResponse(_mapper.Map<CategoryResponseDto>(category), "Category updated successfully");
    }
}
