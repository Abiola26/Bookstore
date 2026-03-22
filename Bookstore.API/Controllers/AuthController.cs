using Bookstore.Application.DTOs;
using Bookstore.Application.Features.Auth.Queries;
using Bookstore.Application.Features.Auth.Commands;
using Bookstore.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookstore.API.Controllers;

/// <summary>
/// Authentication endpoints for user registration and login
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="dto">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("authPolicy")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration request received for a new user");
        var response = await _mediator.Send(new RegisterCommand(dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 201, response);
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("authPolicy")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login request received");
        var response = await _mediator.Send(new LoginCommand(dto), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        _logger.LogInformation("Get profile request for user: {UserId}", userId);
        var response = await _mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return StatusCode(response.StatusCode ?? 200, response);
    }
}
