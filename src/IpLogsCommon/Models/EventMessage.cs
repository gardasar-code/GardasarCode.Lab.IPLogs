#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace IpLogsCommon.Models;

public class EventMessage
{
    public long UserId { get; init; }
    public string IpAddress { get; init; }
    public DateTime EventTime { get; init; }
}