namespace IpLogsCommon.Interfaces;

public interface IEventConsumer : IDisposable
{
    Task Consume(CancellationToken cancellationToken = default);
}