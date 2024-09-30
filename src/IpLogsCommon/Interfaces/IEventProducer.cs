using IpLogsCommon.Models;

namespace IpLogsCommon.Interfaces;

public interface IEventProducer : IDisposable
{
    Task Produce(EventMessage eventMessage, CancellationToken cancellationToken = default);
}