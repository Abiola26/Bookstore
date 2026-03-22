using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<ApiResponse<UserResponseDto>>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserResponseDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<UserResponseDto>.ErrorResponse("User not found", null, 404);

        var response = new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(response);
    }
}
