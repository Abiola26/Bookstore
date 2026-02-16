using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Services;
using Bookstore.Application.Exceptions;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Bookstore.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Microsoft.Extensions.Options.IOptions<Bookstore.Application.Settings.JwtSettings> _jwtOptions;
    private readonly Microsoft.Extensions.Options.IOptions<Bookstore.Application.Settings.EmailSettings> _emailOptions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly UserRegisterDtoValidator _registerValidator;
    private readonly UserLoginDtoValidator _loginValidator;

    public AuthenticationService(IUnitOfWork unitOfWork,
        Microsoft.Extensions.Options.IOptions<Bookstore.Application.Settings.JwtSettings> jwtOptions,
        Microsoft.Extensions.Options.IOptions<Bookstore.Application.Settings.EmailSettings> emailOptions,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions;
        _emailOptions = emailOptions;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _registerValidator = new UserRegisterDtoValidator();
        _loginValidator = new UserLoginDtoValidator();
    }

    public async Task<ApiResponse> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
                return ApiResponse.SuccessResponse("If the email exists, a password reset link will be sent.");

            // Generate reset token
            var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.PasswordResetToken = resetToken;
            var expiryHours = _emailOptions.Value?.PasswordResetTokenExpiryHours ?? 2;
            user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours);

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var frontendOrigin = _emailOptions.Value?.ConfirmationUrlOrigin ?? string.Empty;
            var resetPath = $"/reset-password?userId={user.Id}&token={Uri.EscapeDataString(resetToken)}";
            var resetUrl = string.IsNullOrEmpty(frontendOrigin) ? resetPath : new Uri(new Uri(frontendOrigin), resetPath).ToString();
            var emailBody = $"<p>Hi {user.FullName},</p><p>Reset your password by clicking <a href=\"{resetUrl}\">here</a>. This link expires in {expiryHours} hours.</p>";
            await _emailSender.SendEmailAsync(user.Email, "Password reset", emailBody, cancellationToken);

            return ApiResponse.SuccessResponse("If the email exists, a password reset link will be sent.");
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Password reset request failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse.ErrorResponse("User not found", null, 404);

            if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetTokenExpiresAt == null)
                return ApiResponse.ErrorResponse("Invalid or expired reset token", null, 400);

            if (user.PasswordResetToken != token || user.PasswordResetTokenExpiresAt < DateTimeOffset.UtcNow)
                return ApiResponse.ErrorResponse("Invalid or expired reset token", null, 400);

            // Validate new password
            var errors = Bookstore.Application.Validators.PasswordPolicy.Validate(newPassword);
            if (errors.Count > 0)
                return ApiResponse.ErrorResponse("Password does not meet policy", errors, 400);

            user.UpdatePassword(_passwordHasher.HashPassword(newPassword));
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Password has been reset successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Password reset failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse.ErrorResponse("User not found", null, 404);

            if (!await _passwordHasher.VerifyAsync(currentPassword, user.PasswordHash))
                return ApiResponse.ErrorResponse("Current password is incorrect", null, 401);

            var errors = Bookstore.Application.Validators.PasswordPolicy.Validate(newPassword);
            if (errors.Count > 0)
                return ApiResponse.ErrorResponse("Password does not meet policy", errors, 400);

            user.UpdatePassword(_passwordHasher.HashPassword(newPassword));
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Password changed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Change password failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(UserRegisterDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = _registerValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        // Check if email already exists
        var emailExists = await _unitOfWork.Users.EmailExistsAsync(dto.Email, null, cancellationToken);
        if (emailExists)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Email is already registered", new List<string> { "A user with this email already exists" }, 409);

        try
        {
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            var user = new User(dto.FullName, dto.Email, passwordHash, UserRole.User);
            user.PhoneNumber = dto.PhoneNumber;

            // Generate email confirmation token
            var confirmationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.EmailConfirmationToken = confirmationToken;
            var expiryHours = _emailOptions.Value?.ConfirmationTokenExpiryHours ?? 24;
            user.EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours);

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send confirmation email
            var frontendOrigin = _emailOptions.Value?.ConfirmationUrlOrigin ?? string.Empty;
            var confirmPath = $"/api/email/confirm?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";
            var confirmUrl = string.IsNullOrEmpty(frontendOrigin) ? confirmPath : new Uri(new Uri(frontendOrigin), confirmPath).ToString();
            var emailBody = $"<p>Hi {user.FullName},</p><p>Please confirm your email by clicking <a href=\"{confirmUrl}\">here</a>.</p>";
            await _emailSender.SendEmailAsync(user.Email, "Please confirm your email", emailBody, cancellationToken);

            // Do not issue JWT until email is confirmed
            var response = new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = string.Empty,
                ExpiresAt = DateTime.MinValue
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "User registered successfully. Please confirm your email.", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse($"Registration failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse> ResendConfirmationAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
                return ApiResponse.ErrorResponse("User not found", null, 404);

            if (user.EmailConfirmed)
                return ApiResponse.SuccessResponse("Email already confirmed");

            // Generate new token and update expiry
            var confirmationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.EmailConfirmationToken = confirmationToken;
            var expiryHours = _emailOptions.Value?.ConfirmationTokenExpiryHours ?? 24;
            user.EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours);

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var frontendOrigin = _emailOptions.Value?.ConfirmationUrlOrigin ?? string.Empty;
            var confirmPath = $"/api/email/confirm?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";
            var confirmUrl = string.IsNullOrEmpty(frontendOrigin) ? confirmPath : new Uri(new Uri(frontendOrigin), confirmPath).ToString();
            var emailBody = $"<p>Hi {user.FullName},</p><p>Please confirm your email by clicking <a href=\"{confirmUrl}\">here</a>.</p>";
            await _emailSender.SendEmailAsync(user.Email, "Please confirm your email", emailBody, cancellationToken);

            return ApiResponse.SuccessResponse("Confirmation email resent");
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Resend confirmation failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ApiResponse.ErrorResponse("User not found", null, 404);

            if (user.EmailConfirmed)
                return ApiResponse.SuccessResponse("Email already confirmed");

            if (string.IsNullOrEmpty(user.EmailConfirmationToken) || user.EmailConfirmationTokenExpiresAt == null)
                return ApiResponse.ErrorResponse("No confirmation token found or expired", null, 400);

            if (user.EmailConfirmationToken != token)
                return ApiResponse.ErrorResponse("Invalid confirmation token", null, 400);

            if (user.EmailConfirmationTokenExpiresAt < DateTimeOffset.UtcNow)
                return ApiResponse.ErrorResponse("Confirmation token has expired", null, 400);

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiresAt = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Email confirmed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.ErrorResponse($"Email confirmation failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default)
    {
        var validationErrors = _loginValidator.Validate(dto);
        if (validationErrors.Count > 0)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email, cancellationToken);
            if (user == null)
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password", null, 401);

            if (!user.EmailConfirmed)
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email not confirmed", null, 403);

            if (!await _passwordHasher.VerifyAsync(dto.Password, user.PasswordHash))
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password", null, 401);

            var token = GenerateJwtToken(user.Id, user.Email, user.FullName, user.Role.ToString());

            var response = new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login successful", 200);
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse($"Login failed: {ex.Message}", null, 500);
        }
    }

    public async Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
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
        catch (Exception ex)
        {
            return ApiResponse<UserResponseDto>.ErrorResponse($"Failed to retrieve user: {ex.Message}", null, 500);
        }
    }

    // Password hashing/verification is delegated to IPasswordHasher implementation

    public string GenerateJwtToken(Guid userId, string email, string fullName, string role)
    {
        var jwtKey = _jwtOptions.Value?.Key;
        var jwtIssuer = _jwtOptions.Value?.Issuer;
        var jwtAudience = _jwtOptions.Value?.Audience;
        var expirationMinutes = _jwtOptions.Value?.ExpirationMinutes ?? 1440;

        if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            throw new InvalidOperationException("JWT configuration is missing");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
