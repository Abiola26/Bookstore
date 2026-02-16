namespace Bookstore.Application.Services;

public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string key, int permitLimit, TimeSpan window);
}
