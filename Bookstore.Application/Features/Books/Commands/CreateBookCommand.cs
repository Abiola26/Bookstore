using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;
using Bookstore.Domain.Entities;

namespace Bookstore.Application.Features.Books.Commands;

public record CreateBookCommand(BookCreateDto Dto) : IRequest<ApiResponse<BookResponseDto>>;

public class CreateBookHandler : IRequestHandler<CreateBookCommand, ApiResponse<BookResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBookHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<BookResponseDto>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = _mapper.Map<Book>(request.Dto);
        
        await _unitOfWork.Books.AddAsync(book, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch back with category for response
        var createdBook = await _unitOfWork.Books.GetByIdAsync(book.Id, cancellationToken);
        var responseDto = _mapper.Map<BookResponseDto>(createdBook);

        return ApiResponse<BookResponseDto>.SuccessResponse(responseDto, "Book created successfully", 201);
    }
}
