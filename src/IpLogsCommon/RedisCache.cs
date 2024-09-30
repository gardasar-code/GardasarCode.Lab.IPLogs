using System.Text.Json;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace IpLogsCommon;

public class RedisCache : ICache
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCache>? _logger;
    private readonly IOptionsSnapshot<RedisOptions> _options;
    private readonly ConnectionMultiplexer redis;
    private bool _disposed;

    public RedisCache(IOptionsSnapshot<RedisOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<RedisCache>();
        try
        {
            redis = ConnectionMultiplexer.Connect(options.Value.Connection!);
            _database = redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error connecting to Redis server: {Server}", options.Value.Connection);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, TimeSpan.FromSeconds(_options.Value.Expiration));
            _logger?.LogInformation("Set value cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error set value cache for key: {Key}", key);
            throw;
        }
    }

    public async Task<(bool, T?)> TryGetValue<T>(string key)
    {
        try
        {
            var cachedValue = await _database.StringGetAsync(key);

            if (cachedValue is { HasValue: true, IsNullOrEmpty: false })
            {
                _logger?.LogInformation("Get value from cache for key: {Key}", key);
                return (true, JsonSerializer.Deserialize<T>(cachedValue!));
            }

            return (false, default);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error to get cache for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger?.LogInformation("Remove value from cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing cache for key: {Key}", key);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) redis.Dispose();
        _disposed = true;
    }
}