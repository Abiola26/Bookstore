using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using MediatR;
using Bookstore.Application.Repositories;

namespace Bookstore.Application.Features.Users.Queries;

public record GetAllUsersQuery() : IRequest<ApiResponse<ICollection<UserResponseDto>>>;

public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, ApiResponse<ICollection<UserResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllUsersHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ICollection<UserResponseDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);

        var userDtos = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        }).ToList();

        return ApiResponse<ICollection<UserResponseDto>>.SuccessResponse(userDtos, "Users retrieved successfully");
    }
}
