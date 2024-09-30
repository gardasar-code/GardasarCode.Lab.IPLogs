namespace IpLogsCommon.Models.Options;

public class RedisOptions
{
    public string? Connection { get; set; }
    public int Expiration { get; set; } = 5;
}