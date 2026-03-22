using Bookstore.Application.DTOs;
using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Validators;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using Bookstore.Domain.Entities;
using Bookstore.Domain.Enum;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Bookstore.Application.Features.Auth.Commands;

public record RegisterCommand(UserRegisterDto Dto) : IRequest<ApiResponse<AuthResponseDto>>;

public class RegisterHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterHandler> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;
    private readonly UserRegisterDtoValidator _validator;

    public RegisterHandler(
        IUnitOfWork unitOfWork,
        ILogger<RegisterHandler> logger,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
        _validator = new UserRegisterDtoValidator();
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request.Dto);
        if (validationErrors.Count > 0)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse(validationErrors[0], validationErrors, 400);
        }

        var emailExists = await _unitOfWork.Users.EmailExistsAsync(request.Dto.Email, null, cancellationToken);
        if (emailExists)
            return ApiResponse<AuthResponseDto>.ErrorResponse("Email is already registered", new List<string> { "A user with this email already exists" }, 409);

        var passwordHash = _passwordHasher.HashPassword(request.Dto.Password);
        var user = new User(request.Dto.FullName, request.Dto.Email, passwordHash, UserRole.User);
        user.PhoneNumber = request.Dto.PhoneNumber;

        var confirmationToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.EmailConfirmationToken = confirmationToken;
        var expiryHours = _emailOptions.Value?.ConfirmationTokenExpiryHours ?? 24;
        user.EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var content = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h1 style='color: #007bff; margin: 0;'>📚 Bookstore</h1>
                </div>
                <h2 style='color: #333; text-align: center;'>Welcome to Bookstore!</h2>
                <div style='color: #444; line-height: 1.6;'>
                    <p>Hi <strong>{user.FullName}</strong>,</p>
                    <p>Welcome to our community of book lovers! We're excited to have you on board.</p>
                    <p>To get started, please confirm your email address by using the following 6-digit authorization code:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <div style='background-color: #f8f9fa; color: #333; padding: 15px 30px; border-radius: 8px; font-weight: bold; font-size: 28px; display: inline-block; letter-spacing: 5px; border: 2px dashed #007bff;'>
                            {confirmationToken}
                        </div>
                    </div>
                    <p style='color: #666; font-size: 14px;'>This code will expire in {expiryHours} hours.</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                <p style='color: #999; font-size: 12px; text-align: center;'>&copy; {DateTime.Now.Year} Bookstore Inc. All rights reserved.</p>
            </div>";

        await _emailSender.SendEmailAsync(user.Email, "Confirm your email - Bookstore", content, cancellationToken);

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
}
