using Bookstore.Application.DTOs;
using Bookstore.Application.Services;
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
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="dto">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with JWT token</returns>
    /// <response code="201">User registered successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="409">Email already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("authPolicy")]  // Prevent brute force
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration request received for a new user");
        var response = await _authService.RegisterAsync(dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with JWT token</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("authPolicy")]  // Prevent brute force
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login request received");
        var response = await _authService.LoginAsync(dto, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user details</returns>
    /// <response code="200">User profile retrieved</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Bookstore.Application.Common.ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        _logger.LogInformation("Get profile request for user: {UserId}", userId);
        var response = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return StatusCode(response.StatusCode ?? 400, response);
    }
}
