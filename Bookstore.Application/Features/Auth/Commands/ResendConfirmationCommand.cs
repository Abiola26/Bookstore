using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Bookstore.Application.Features.Auth.Commands;

public record ResendConfirmationCommand(string Email) : IRequest<ApiResponse>;

public class ResendConfirmationHandler : IRequestHandler<ResendConfirmationCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResendConfirmationHandler> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;

    public ResendConfirmationHandler(
        IUnitOfWork unitOfWork,
        ILogger<ResendConfirmationHandler> logger,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
    }

    public async Task<ApiResponse> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return ApiResponse.ErrorResponse("User not found", null, 404);

        if (user.EmailConfirmed)
            return ApiResponse.SuccessResponse("Email already confirmed");

        var confirmationToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.EmailConfirmationToken = confirmationToken;
        var expiryHours = _emailOptions.Value?.ConfirmationTokenExpiryHours ?? 24;
        user.EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var content = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h1 style='color: #007bff; margin: 0;'>📚 Bookstore</h1>
                </div>
                <h2 style='color: #333; text-align: center;'>Email Confirmation</h2>
                <div style='color: #444; line-height: 1.6;'>
                    <p>Hi <strong>{user.FullName}</strong>,</p>
                    <p>You requested a new confirmation code for your email address.</p>
                    <p>Please use the following 6-digit authorization code to confirm your email:</p>
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

        return ApiResponse.SuccessResponse("Confirmation email resent");
    }
}
