using IpLogsCommon.Models;

namespace IpLogsCommon.Interfaces;

public interface IEventGenerator
{
    EventMessage Generate();
}