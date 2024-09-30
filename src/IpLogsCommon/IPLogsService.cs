using System.Runtime.CompilerServices;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;
using IpLogsCommon.Repository.Entities;
using IpLogsCommon.Repository.Interfaces;
using IpLogsCommon.Repository.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IpLogsCommon;

public class IPLogsService : IIPLogsService
{
    private readonly ICache _cache;
    private readonly ILogger<IPLogsService>? _logger;
    private readonly IRepository<DbContext> _repo;

    public IPLogsService(IRepository<DbContext> repo, ICache cache, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<IPLogsService>();
        _repo = repo;
        _cache = cache;
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

        var existingUser = await _repo.FirstOrDefaultAsync(new UserSpecification.GetUserById(newUser.Id, false),
            cancellationToken);

        if (existingUser != null)
            _repo.SetValues(existingUser, newUser, cancellationToken);
        else
            await _repo.AddAsync(newUser, cancellationToken);

        await _repo.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync($"{userId}_{nameof(GetLastConnectionAsync)}");
        await _cache.RemoveAsync($"{userId}_{nameof(GetUserIPsStream)}");

        _logger?.LogInformation("User connection added/updated successfully for userId: {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task<UserLastConnection> GetLastConnectionAsync(long userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{userId}_{nameof(GetLastConnectionAsync)}";

        var (cached, userLastConnection) = await _cache.TryGetValueAsync<UserLastConnection>(cacheKey);
        if (cached) return userLastConnection ?? new UserLastConnection();

        var user = await _repo.FirstOrDefaultAsync(new UserSpecification.GetUserById(userId, true),
            cancellationToken);

        userLastConnection = new UserLastConnection
            { LastConnectionTime = user?.LastConnectionTime, IPAddress = user?.IPAddress };
        await _cache.SetAsync(cacheKey, userLastConnection);

        _logger?.LogWarning(
            user == null ? "User not found for userId: {UserId}" : "User found for userId: {UserId}",
            userId);

        return userLastConnection;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetUserIPsStream(long userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{userId}_{nameof(GetUserIPsStream)}";

        var (cached, cachedIps) = await _cache.TryGetValueAsync<List<string>>(cacheKey);

        if (!cached)
        {
            cachedIps = new List<string>();

            await foreach (var ip in _repo.AsAsyncEnumerableStream(
                               new UserSpecification.GetUserIpsById(userId, true), cancellationToken))
            {
                cachedIps.Add(ip);
                yield return ip;
            }

            await _cache.SetAsync(cacheKey, cachedIps);
        }
        else
        {
            if (cachedIps == null) yield break;

            foreach (var ip in cachedIps) yield return ip;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<long> FindUsersByIpPartStream(string ipPart,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ipPart}_{nameof(FindUsersByIpPartStream)}";

        var (cached, cachedIps) = await _cache.TryGetValueAsync<List<long>>(cacheKey);

        if (!cached)
        {
            cachedIps = new List<long>();

            await foreach (var id in _repo.AsAsyncEnumerableStream(new UserSpecification.FindUsersByIpPart(ipPart, true),
                               cancellationToken))
            {
                cachedIps.Add(id);
                yield return id;
            }

            await _cache.SetAsync(cacheKey, cachedIps);
        }
        else
        {
            if (cachedIps == null) yield break;

            foreach (var id in cachedIps) yield return id;
        }
    }
}