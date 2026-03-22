using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bookstore.Application.Features.Auth.Commands;

public record LoginCommand(UserLoginDto Dto) : IRequest<ApiResponse<AuthResponseDto>>;

public class LoginHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly UserLoginDtoValidator _validator;

    public LoginHandler(
        IUnitOfWork unitOfWork,
        ILogger<LoginHandler> logger,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _validator = new UserLoginDtoValidator();
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request.Dto);
        if (validationErrors.Count > 0)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Validation failed", validationErrors, 400);

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Dto.Email, cancellationToken);
        if (user == null)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password", null, 401);

        if (!user.EmailConfirmed)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Email not confirmed", null, 403);

        if (!await _passwordHasher.VerifyAsync(request.Dto.Password, user.PasswordHash))
            return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password", null, 401);

        var token = _jwtProvider.GenerateJwtToken(user.Id, user.Email, user.FullName, user.Role.ToString());

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
}
