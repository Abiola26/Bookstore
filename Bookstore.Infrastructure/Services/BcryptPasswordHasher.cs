using Bookstore.Application.Services;

namespace Bookstore.Infrastructure.Services;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public Task<bool> VerifyAsync(string password, string hash)
    {
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, hash));
    }
}
