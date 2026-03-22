using Bookstore.Application.Common;
using Bookstore.Application.Repositories;
using Bookstore.Application.Services;
using Bookstore.Application.Validators;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookstore.Application.Features.Auth.Commands;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<ApiResponse>;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApiResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordHandler> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordHandler> logger,
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse.ErrorResponse("User not found", null, 404);

        if (!await _passwordHasher.VerifyAsync(request.CurrentPassword, user.PasswordHash))
            return ApiResponse.ErrorResponse("Current password is incorrect", null, 401);

        var errors = PasswordPolicy.Validate(request.NewPassword);
        if (errors.Count > 0)
            return ApiResponse.ErrorResponse("Password does not meet policy", errors, 400);

        user.UpdatePassword(_passwordHasher.HashPassword(request.NewPassword));
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResponse("Password changed successfully");
    }
}
