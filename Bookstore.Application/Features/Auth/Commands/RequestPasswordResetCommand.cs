using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Bookstore.Application.Features.Auth.Commands;

public record RequestPasswordResetCommand(string Email) : IRequest<ApiResponse>;

public class RequestPasswordResetHandler : IRequestHandler<RequestPasswordResetCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestPasswordResetHandler> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;

    public RequestPasswordResetHandler(
        IUnitOfWork unitOfWork,
        ILogger<RequestPasswordResetHandler> logger,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
    }

    public async Task<ApiResponse> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return ApiResponse.SuccessResponse("If the email exists, a password reset code will be sent.");

        var resetToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.PasswordResetToken = resetToken;
        var expiryHours = _emailOptions.Value?.PasswordResetTokenExpiryHours ?? 2;
        user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(expiryHours);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var content = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h1 style='color: #007bff; margin: 0;'>📚 Bookstore</h1>
                </div>
                <h2 style='color: #333; text-align: center;'>Reset Your Password</h2>
                <div style='color: #444; line-height: 1.6;'>
                    <p>Hi <strong>{user.FullName}</strong>,</p>
                    <p>We received a request to reset your password. If you didn't make this request, you can safely ignore this email.</p>
                    <p>To reset your password, please use the following 6-digit authorization code within the next <strong>{expiryHours} hours</strong>:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <div style='background-color: #f8f9fa; color: #333; padding: 15px 30px; border-radius: 8px; font-weight: bold; font-size: 28px; display: inline-block; letter-spacing: 5px; border: 2px dashed #dc3545;'>
                            {resetToken}
                        </div>
                    </div>
                    <p style='color: #666; font-size: 14px;'>Enter this code on the password reset page to choose a new password.</p>
                </div>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                <p style='color: #999; font-size: 12px; text-align: center;'>&copy; {DateTime.Now.Year} Bookstore Inc. All rights reserved.</p>
            </div>";

        await _emailSender.SendEmailAsync(user.Email, "Reset Your Password - Bookstore", content, cancellationToken);

        return ApiResponse.SuccessResponse("If the email exists, a password reset code will be sent.");
    }
}
