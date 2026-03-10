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
    /// Confirms email via a 6-digit OTP sent to the user's email.
    /// </summary>
    [HttpPost("confirm-otp")]
    public async Task<IActionResult> ConfirmEmailOtp([FromBody] ConfirmEmailDto dto)
    {
        if (dto == null || dto.UserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Invalid request: userId and token are required", null, 400));

        var response = await _authService.ConfirmEmailAsync(dto.UserId, dto.Token);
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

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(Bookstore.Application.Common.ApiResponse.ErrorResponse("Invalid request", null, 400));
        var response = await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
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
