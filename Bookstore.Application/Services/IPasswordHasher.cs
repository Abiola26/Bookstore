namespace Bookstore.Application.Services;

public interface IPasswordHasher
{
    string HashPassword(string password);
    Task<bool> VerifyAsync(string password, string hash);
}
