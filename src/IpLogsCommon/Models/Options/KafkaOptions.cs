namespace IpLogsCommon.Models.Options;

public class KafkaOptions
{
    public string? Broker { get; set; }
    public string? Topic { get; set; }
    public string? ConsumerGroup { get; set; }
}