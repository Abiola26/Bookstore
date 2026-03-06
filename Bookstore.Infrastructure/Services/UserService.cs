using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Bookstore.Application.Repositories;
using Microsoft.Extensions.Logging;
using Bookstore.Domain.Enum;

namespace Bookstore.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ICollection<UserResponseDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all users for administrative purposes");
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

    public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
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

    public async Task<ApiResponse<UserResponseDto>> UpdateUserRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Administrative action: Updating role for user {UserId} to {Role}", id, role);
        
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return ApiResponse<UserResponseDto>.ErrorResponse("User not found", null, 404);

        user.Role = role;
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

        return ApiResponse<UserResponseDto>.SuccessResponse(dto, $"User role updated to {role}");
    }

    public async Task<ApiResponse> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogCritical("Administrative action: Deleting user {UserId}", id);
        
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return ApiResponse.ErrorResponse("User not found", null, 404);

        _unitOfWork.Users.Delete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("User deleted successfully");
    }
}
