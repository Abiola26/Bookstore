using Bookstore.Application.Services;
using Bookstore.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.API.Controllers;

/// <summary>
/// Administrative endpoints for user management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Only Admins can access these endpoints
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users in the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<ICollection<Application.DTOs.UserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var response = await _userService.GetAllUsersAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<Application.DTOs.UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _userService.GetUserByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Update a user's role (Promote to Admin or Demote to User)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="role">New role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user details</returns>
    public class UserRoleUpdateDto { public UserRole Role { get; set; } }

    [HttpPatch("{id}/role")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse<Application.DTOs.UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UserRoleUpdateDto dto, CancellationToken cancellationToken)
    {
        var response = await _userService.UpdateUserRoleAsync(id, dto.Role, cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Delete a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Application.Common.ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var response = await _userService.DeleteUserAsync(id, cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }
}
