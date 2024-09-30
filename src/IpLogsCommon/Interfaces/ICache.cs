namespace IpLogsCommon.Interfaces;

public interface ICache : IDisposable
{
    Task<(bool, T?)> TryGetValueAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
}