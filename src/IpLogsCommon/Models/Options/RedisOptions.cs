namespace IpLogsCommon.Models.Options;

public class RedisOptions
{
    public string? Connection { get; init; }
    public int Expiration { get; init; } = 5;
}