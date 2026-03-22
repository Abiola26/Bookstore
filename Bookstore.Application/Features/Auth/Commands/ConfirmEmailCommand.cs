using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Application.Features.Auth.Commands;

public record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<ApiResponse>;

public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmEmailHandler> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailSettings> _emailOptions;

    public ConfirmEmailHandler(
        IUnitOfWork unitOfWork,
        ILogger<ConfirmEmailHandler> logger,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailOptions)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
    }

    public async Task<ApiResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse.ErrorResponse("User not found", null, 404);

        if (user.EmailConfirmed)
            return ApiResponse.SuccessResponse("Email already confirmed");

        if (string.IsNullOrEmpty(user.EmailConfirmationToken) || user.EmailConfirmationTokenExpiresAt == null)
            return ApiResponse.ErrorResponse("No confirmation token found or expired", null, 400);

        if (user.EmailConfirmationToken != request.Token)
            return ApiResponse.ErrorResponse("Invalid confirmation token", null, 400);

        if (user.EmailConfirmationTokenExpiresAt < DateTimeOffset.UtcNow)
            return ApiResponse.ErrorResponse("Confirmation token has expired", null, 400);

        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiresAt = null;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var welcomeContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h1 style='color: #007bff; margin: 0;'>📚 Bookstore</h1>
                </div>
                <h2 style='color: #333; text-align: center;'>Your Account is Ready!</h2>
                <div style='color: #444; line-height: 1.6;'>
                    <p>Hi <strong>{user.FullName}</strong>,</p>
                    <p>Your email has been successfully confirmed. Welcome to <strong>Bookstore</strong>!</p>
                    <p>You now have full access to our collection. Feel free to browse books, add them to your cart, and start reading.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{_emailOptions.Value?.ConfirmationUrlOrigin}' style='background-color: #28a745; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Start Browsing</a>
                    </div>
                </div>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                <p style='color: #999; font-size: 12px; text-align: center;'>&copy; {DateTime.Now.Year} Bookstore Inc. All rights reserved.</p>
            </div>";

        await _emailSender.SendEmailAsync(user.Email, "Account Activated - Welcome to Bookstore", welcomeContent, cancellationToken);

        return ApiResponse.SuccessResponse("Email confirmed successfully");
    }
}
