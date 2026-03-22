using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Domain.Enum;
using MediatR;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Users.Commands;

public record UpdateUserRoleCommand(Guid Id, UserRole Role) : IRequest<ApiResponse<UserResponseDto>>;

public class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand, ApiResponse<UserResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserRoleHandler> _logger;

    public UpdateUserRoleHandler(IUnitOfWork unitOfWork, ILogger<UpdateUserRoleHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<UserResponseDto>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Administrative action: Updating role for user {UserId} to {Role}", request.Id, request.Role);

        var user = await _unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return ApiResponse<UserResponseDto>.ErrorResponse("User not found", null, 404);

        user.Role = request.Role;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(dto, $"User role updated to {request.Role}");
    }
}
