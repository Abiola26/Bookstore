using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Books.Commands;

public record UpdateBookCommand(Guid Id, BookUpdateDto Dto) : IRequest<ApiResponse<BookResponseDto>>;

public class UpdateBookHandler : IRequestHandler<UpdateBookCommand, ApiResponse<BookResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBookHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<BookResponseDto>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(request.Id, cancellationToken);
        if (book == null)
            return ApiResponse<BookResponseDto>.ErrorResponse("Book not found", null, 404);

        // Update properties
        if (request.Dto.Title != null) book.Title = request.Dto.Title;
        if (request.Dto.Author != null) book.Author = request.Dto.Author;
        if (request.Dto.Description != null) book.Description = request.Dto.Description;
        if (request.Dto.TotalQuantity.HasValue)
        {
            book.TotalQuantity = request.Dto.TotalQuantity.Value;
        }
        if (request.Dto.CategoryId.HasValue)
        {
            book.CategoryId = request.Dto.CategoryId.Value;
        }
        
        if (request.Dto.Price.HasValue && request.Dto.Price.Value > 0)
        {
             book.Price = new Bookstore.Domain.ValueObjects.Money(request.Dto.Price.Value, request.Dto.Currency ?? "USD");
        }

        _unitOfWork.Books.Update(book);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedBook = await _unitOfWork.Books.GetByIdAsync(book.Id, cancellationToken);
        return ApiResponse<BookResponseDto>.SuccessResponse(_mapper.Map<BookResponseDto>(updatedBook), "Book updated successfully");
    }
}
