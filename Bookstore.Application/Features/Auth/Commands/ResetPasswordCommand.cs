using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Validators;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ApiResponse>;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordHandler> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordHandler> logger,
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return ApiResponse.ErrorResponse("User not found", null, 404);

        if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetTokenExpiresAt == null)
            return ApiResponse.ErrorResponse("Invalid or expired reset code", null, 400);

        if (user.PasswordResetToken != request.Token || user.PasswordResetTokenExpiresAt < DateTimeOffset.UtcNow)
            return ApiResponse.ErrorResponse("Invalid or expired reset code", null, 400);

        var errors = PasswordPolicy.Validate(request.NewPassword);
        if (errors.Count > 0)
            return ApiResponse.ErrorResponse("Password does not meet policy", errors, 400);

        user.UpdatePassword(_passwordHasher.HashPassword(request.NewPassword));
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Password has been reset successfully");
    }
}
