using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Categories.Commands;

public record DeleteCategoryCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
            return ApiResponse.ErrorResponse("Category not found", null, 404);

        _unitOfWork.Categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Category deleted successfully");
    }
}
