using Bookstore.Application.Services;

namespace Bookstore.Tests.Integration;

public class MockRateLimitService : IRateLimitService
{
    public Task<bool> IsAllowedAsync(string key, int permitLimit, TimeSpan window)
    {
        return Task.FromResult(true);
    }
}
