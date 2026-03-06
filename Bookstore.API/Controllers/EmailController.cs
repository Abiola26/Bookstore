using Bookstore.Application.Services;
using Bookstore.Application.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.API.Controllers;

[ApiController]
[EnableRateLimiting("emailPolicy")]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmailController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public EmailController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Confirms email via browser link from email. Redirects to frontend when done.
    /// Used when the confirmation link in the email points directly to the backend.
    /// </summary>
    [HttpGet("confirm")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var response = await _authService.ConfirmEmailAsync(userId, token);
        
        var frontendUrl = _authService.GetFrontendOrigin();
        if (response.Success)
        {
            return Redirect($"{frontendUrl}/auth/login?confirmed=true");
        }
        
        return Redirect($"{frontendUrl}/auth/login?error={Uri.EscapeDataString(response.Message)}");
    }

    /// <summary>
    /// Confirms email and returns a JSON response (no redirect).
    /// Used by the frontend /verify-email page to verify tokens without cross-origin redirect issues.
    /// </summary>
    [HttpGet("confirm-json")]
    public async Task<IActionResult> ConfirmEmailJson([FromQuery] Guid userId, [FromQuery] string token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Invalid request: userId and token are required", null, 400));

        var response = await _authService.ConfirmEmailAsync(userId, token);
        return StatusCode(response.StatusCode ?? (response.Success ? 200 : 400), response);
    }

    [HttpPost("resend")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Email is required", null, 400));

        var response = await _authService.ResendConfirmationAsync(dto.Email);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    [HttpPost("password/request-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Email is required", null, 400));

        var response = await _authService.RequestPasswordResetAsync(dto.Email);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    [HttpPost("password/resend")]
    [EnableRateLimiting("emailPolicy")]
    public async Task<IActionResult> ResendPasswordReset([FromBody] PasswordResetRequestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Email is required", null, 400));

        var response = await _authService.RequestPasswordResetAsync(dto.Email);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto dto)
    {
        if (dto == null || dto.UserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Invalid request", null, 400));
        var response = await _authService.ResetPasswordAsync(dto.UserId, dto.Token, dto.NewPassword);
        return StatusCode(response.StatusCode ?? 400, response);
    }

    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (dto == null || dto.UserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Invalid request", null, 400));

        var response = await _authService.ChangePasswordAsync(dto.UserId, dto.CurrentPassword, dto.NewPassword);
        return StatusCode(response.StatusCode ?? 400, response);
    }
}
