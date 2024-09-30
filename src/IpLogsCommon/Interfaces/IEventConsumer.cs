namespace IpLogsCommon.Interfaces;

public interface IEventConsumer : IDisposable
{
    Task ConsumeAsync(CancellationToken cancellationToken = default);
}