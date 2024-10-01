namespace IpLogsCommon.Models.Options;

public class KafkaOptions
{
    public string? Broker { get; init; }
    public string? Topic { get; init; }
    public string? ConsumerGroup { get; init; }
}