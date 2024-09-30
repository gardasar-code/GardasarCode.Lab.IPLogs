using IpLogsCommon.Models;

namespace IpLogsCommon.Interfaces;

public interface IEventProducer : IDisposable
{
    Task ProduceAsync(EventMessage eventMessage, CancellationToken cancellationToken = default);
}