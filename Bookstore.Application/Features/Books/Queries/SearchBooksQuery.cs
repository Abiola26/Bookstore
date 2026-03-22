using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Books.Queries;

public record SearchBooksQuery(string Title) : IRequest<ApiResponse<ICollection<BookResponseDto>>>;

public class SearchBooksHandler : IRequestHandler<SearchBooksQuery, ApiResponse<ICollection<BookResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SearchBooksHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ICollection<BookResponseDto>>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ApiResponse<ICollection<BookResponseDto>>.ErrorResponse("Search title is required", null, 400);

        var books = await _unitOfWork.Books.SearchByTitleAsync(request.Title, cancellationToken);
        var dtos = _mapper.Map<ICollection<BookResponseDto>>(books);
        return ApiResponse<ICollection<BookResponseDto>>.SuccessResponse(dtos);
    }
}
