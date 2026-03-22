using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<ApiResponse<UserResponseDto>>;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserResponseDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return ApiResponse<UserResponseDto>.ErrorResponse("User not found", null, 404);

        var dto = new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(dto);
    }
}
