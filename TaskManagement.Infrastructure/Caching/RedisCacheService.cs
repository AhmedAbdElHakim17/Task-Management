using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var db = connectionMultiplexer.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        var db = connectionMultiplexer.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        var db = connectionMultiplexer.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}
