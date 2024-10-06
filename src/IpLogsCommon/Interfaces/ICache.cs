namespace IpLogsCommon.Interfaces;

public interface ICache : IDisposable, IAsyncDisposable
{
    Task<(bool, T?)> TryGetValueAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
}