namespace IpLogsCommon.Interfaces;

public interface ICache : IDisposable
{
    Task<(bool, T?)> TryGetValue<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
}