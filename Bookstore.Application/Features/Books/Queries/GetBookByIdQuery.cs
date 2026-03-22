using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
using AutoMapper;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Books.Queries;

public record GetBookByIdQuery(Guid Id) : IRequest<ApiResponse<BookResponseDto>>;

public class GetBookByIdHandler : IRequestHandler<GetBookByIdQuery, ApiResponse<BookResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookByIdHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<BookResponseDto>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(request.Id, cancellationToken);
        if (book == null)
            return ApiResponse<BookResponseDto>.ErrorResponse("Book not found", null, 404);

        return ApiResponse<BookResponseDto>.SuccessResponse(_mapper.Map<BookResponseDto>(book));
    }
}
