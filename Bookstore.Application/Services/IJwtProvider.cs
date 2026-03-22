namespace Bookstore.Application.Services;

public interface IJwtProvider
{
    string GenerateJwtToken(Guid userId, string email, string fullName, string role);
}
