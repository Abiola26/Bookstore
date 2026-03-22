using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Books.Commands;

public record DeleteBookCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteBookHandler : IRequestHandler<DeleteBookCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(request.Id, cancellationToken);
        if (book == null)
            return ApiResponse.ErrorResponse("Book not found", null, 404);

        await _unitOfWork.Books.DeleteWithRelatedAsync(book, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Book deleted successfully");
    }
}
