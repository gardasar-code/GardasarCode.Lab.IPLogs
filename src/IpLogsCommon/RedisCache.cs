using System.Text.Json;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace IpLogsCommon;

public sealed class RedisCache : ICache
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCache>? _logger;
    private readonly IOptionsSnapshot<RedisOptions> _options;
    private readonly ConnectionMultiplexer _redis;

    public RedisCache(IOptionsSnapshot<RedisOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<RedisCache>();
        try
        {
            _redis = ConnectionMultiplexer.Connect(options.Value.Connection!);
            _database = _redis.GetDatabase();
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
            await _database.StringSetAsync(key, serializedValue, TimeSpan.FromSeconds(_options.Value.Expiration))
                .ConfigureAwait(false);
            _logger?.LogInformation("Set value cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error set value cache for key: {Key}", key);
            throw;
        }
    }

    public async Task<(bool, T?)> TryGetValueAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _database.StringGetAsync(key).ConfigureAwait(false);

            if (cachedValue is not { HasValue: true, IsNullOrEmpty: false }) return (false, default);
            _logger?.LogInformation("Get value from cache for key: {Key}", key);
            return (true, JsonSerializer.Deserialize<T>(cachedValue!));
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
            await _database.KeyDeleteAsync(key).ConfigureAwait(false);
            _logger?.LogInformation("Remove value from cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing cache for key: {Key}", key);
            throw;
        }
    }

    #region Dispose

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        ReleaseUnmanagedResources();
        if (disposing) _redis.Dispose();

        _disposed = true;
    }

    private async ValueTask DisposeAsyncCore(bool disposing)
    {
        if (_disposed) return;

        ReleaseUnmanagedResources();
        if (disposing) await _redis.DisposeAsync().ConfigureAwait(false);

        _disposed = true;
    }

    private void ReleaseUnmanagedResources()
    {
    }

    ~RedisCache()
    {
        Dispose(false);
    }

    #endregion
}