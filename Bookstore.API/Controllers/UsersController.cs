using Bookstore.Application.Features.Users.Queries;
using Bookstore.Application.Features.Users.Commands;
using Bookstore.Domain.Enum;
using Bookstore.Application.Common;
using Bookstore.Application.DTOs;
using MediatR;
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
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all users in the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ICollection<UserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Update a user's role (Promote to Admin or Demote to User)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">New role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user details</returns>
    public class UserRoleUpdateDto { public UserRole Role { get; set; } }

    [HttpPatch("{id}/role")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UserRoleUpdateDto dto, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new UpdateUserRoleCommand(id, dto.Role), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Delete a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }
}
