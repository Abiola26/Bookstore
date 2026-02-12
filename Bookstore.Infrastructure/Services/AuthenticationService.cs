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
    private readonly IConfiguration _configuration;
    private readonly UserRegisterDtoValidator _registerValidator;
    private readonly UserLoginDtoValidator _loginValidator;

    public AuthenticationService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _registerValidator = new UserRegisterDtoValidator();
        _loginValidator = new UserLoginDtoValidator();
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
            var passwordHash = HashPassword(dto.Password);
            var user = new User(dto.FullName, dto.Email, passwordHash, UserRole.User);
            user.PhoneNumber = dto.PhoneNumber;

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

            return ApiResponse<AuthResponseDto>.SuccessResponse(response, "User registered successfully", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse($"Registration failed: {ex.Message}", null, 500);
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

            if (!await VerifyPasswordAsync(dto.Password, user.PasswordHash))
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

    public async Task<bool> VerifyPasswordAsync(string password, string hash)
    {
        return await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, hash));
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public string GenerateJwtToken(Guid userId, string email, string fullName, string role)
    {
        var jwtKey = _configuration["JWT:Key"];
        var jwtIssuer = _configuration["JWT:Issuer"];
        var jwtAudience = _configuration["JWT:Audience"];

        if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            throw new InvalidOperationException("JWT configuration is missing");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
