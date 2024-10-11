using System.Runtime.CompilerServices;
using GardasarCode.Repository.Interfaces;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;
using IpLogsCommon.Repository.Entities;
using IpLogsCommon.Repository.Specifications;
using Microsoft.Extensions.Logging;

namespace IpLogsCommon;

public class IPLogsService(IRepository repo, ICache cache, ILoggerFactory loggerFactory)
    : IIPLogsService
{
    private readonly ILogger<IPLogsService>? _logger = loggerFactory.CreateLogger<IPLogsService>();

    private static readonly SemaphoreSlim SemaphoreFindUsersByIpPartStream = new(1, 1);

    private static readonly SemaphoreSlim SemaphoreGetLastConnectionAsync = new(1, 1);

    private static readonly SemaphoreSlim SemaphoreGetUserIPsStream = new(1, 1);

    public static void DisposeSemaphores()
    {
        SemaphoreFindUsersByIpPartStream.Dispose();
        SemaphoreGetLastConnectionAsync.Dispose();
        SemaphoreGetUserIPsStream.Dispose();
    }
    /// <inheritdoc />
    public async Task AddConnectionAsync(long userId, string ipAddress, DateTime eventTime,
        CancellationToken cancellationToken = default)
    {
        var newUser = new User
        {
            Id = userId,
            IPAddress = ipAddress,
            LastConnectionTime = eventTime.ToUniversalTime()
        };

        var existingUser = await repo.FirstOrDefaultAsync(new UserSpecification.GetUserById(newUser.Id, false),
            cancellationToken);

        if (existingUser != null)
            repo.SetValues(existingUser, newUser);
        else
            await repo.AddAsync(newUser, cancellationToken).ConfigureAwait(false);

        await repo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cache.RemoveAsync($"{userId}_{nameof(GetLastConnectionAsync)}").ConfigureAwait(false);
        await cache.RemoveAsync($"{userId}_{nameof(GetUserIPsStream)}").ConfigureAwait(false);

        _logger?.LogInformation("User connection added/updated successfully for userId: {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task<UserLastConnection> GetLastConnectionAsync(long userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{userId}_{nameof(GetLastConnectionAsync)}";
        var spec = new UserSpecification.GetUserById(userId, true);
        
        var result = await TryGetCachedFirstOrDefaultAsync(cacheKey, spec, SemaphoreGetLastConnectionAsync, (Func<User?, UserLastConnection>)Selector, cancellationToken);

        string message = result == null ? "User not found for userId: {UserId}" : "User found for userId: {UserId}";
        _logger?.LogWarning(message, userId);
        
        return result ?? new UserLastConnection();
        
        UserLastConnection Selector(User? user)
        {
            return new UserLastConnection { LastConnectionTime = user?.LastConnectionTime, IPAddress = user?.IPAddress };
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetUserIPsStream(long userId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{userId}_{nameof(GetUserIPsStream)}";
        var spec = new UserSpecification.GetUserIpsById(userId, true);

        await foreach (var item in GetCachedStream(cacheKey, spec, SemaphoreGetUserIPsStream, cancellationToken).ConfigureAwait(false)) yield return item;
    }

    public async IAsyncEnumerable<long> FindUsersByIpPartStream(string ipPart, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var spec = new UserSpecification.FindUsersByIpPart(ipPart, true);
        var cacheKey = $"{ipPart}_{nameof(FindUsersByIpPartStream)}";

        await foreach (var item in GetCachedStream(cacheKey, spec, SemaphoreFindUsersByIpPartStream, cancellationToken).ConfigureAwait(false)) yield return item;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="spec"></param>
    /// <param name="semaphore"></param>
    /// <param name="selector"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    private async Task<TResult?> TryGetCachedFirstOrDefaultAsync<T, TResult>(string cacheKey, ISpecification<T> spec, SemaphoreSlim semaphore, Func<T?, TResult> selector, CancellationToken cancellationToken = default) where T : class
    {
        var (cached, cachedResult) = await cache.TryGetValueAsync<TResult>(cacheKey).ConfigureAwait(false);
        if (cached) return cachedResult;

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            (cached, cachedResult) = await cache.TryGetValueAsync<TResult>(cacheKey).ConfigureAwait(false);
            if (cached) return cachedResult;

            var result = await repo.FirstOrDefaultAsync(spec, cancellationToken).ConfigureAwait(false);

            cachedResult = selector.Invoke(result);

            await cache.SetAsync(cacheKey, cachedResult).ConfigureAwait(false);
            return cachedResult;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="spec"></param>
    /// <param name="semaphore"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    private async IAsyncEnumerable<TResult> GetCachedStream<T, TResult>(string cacheKey, ISpecification<T, TResult> spec, SemaphoreSlim semaphore, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class
    {
        var (isCached, cachedResult) = await cache.TryGetValueAsync<List<TResult>>(cacheKey).ConfigureAwait(false);

        if (isCached)
        {
            if (cachedResult == null) yield break;
            foreach (var item in cachedResult) yield return item;
        }
        else
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                (isCached, cachedResult) = await cache.TryGetValueAsync<List<TResult>>(cacheKey).ConfigureAwait(false);

                if (isCached)
                {
                    if (cachedResult == null) yield break;
                    foreach (var ip in cachedResult) yield return ip;
                }

                cachedResult = [];

                await foreach (var item in repo.AsAsyncEnumerableStream(spec, cancellationToken).ConfigureAwait(false))
                {
                    cachedResult.Add(item);
                    yield return item;
                }

                await cache.SetAsync(cacheKey, cachedResult).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}