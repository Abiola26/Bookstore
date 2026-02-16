using Bookstore.Application.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Bookstore.Infrastructure.Services;

public class DistributedRateLimitService : IRateLimitService
{
    private readonly IDistributedCache _cache;

    public DistributedRateLimitService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> IsAllowedAsync(string key, int permitLimit, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var entryKey = $"ratelimit:{key}";
        var existing = await _cache.GetStringAsync(entryKey);
        var record = existing != null ? JsonSerializer.Deserialize<RateLimitRecord>(existing) : null;

        if (record == null || record.WindowStart + window <= now)
        {
            record = new RateLimitRecord { WindowStart = now, Count = 1 };
            await _cache.SetStringAsync(entryKey, JsonSerializer.Serialize(record), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window });
            return true;
        }

        if (record.Count < permitLimit)
        {
            record.Count++;
            await _cache.SetStringAsync(entryKey, JsonSerializer.Serialize(record), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window });
            return true;
        }

        return false;
    }

    private class RateLimitRecord
    {
        public DateTimeOffset WindowStart { get; set; }
        public int Count { get; set; }
    }
}
