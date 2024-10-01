using System.Text.Json.Serialization;

namespace IpLogsCommon.Models;

#pragma warning disable CS8618
public class UserLastConnection
{
    [JsonPropertyName("ip_address")]
    // ReSharper disable once InconsistentNaming
    public string? IPAddress { get; init; } = string.Empty;

    [JsonPropertyName("last_connection_time")]
    public DateTime? LastConnectionTime { get; init; }
}