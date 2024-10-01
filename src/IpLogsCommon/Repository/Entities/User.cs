namespace IpLogsCommon.Repository.Entities;

public class User
{
    public long Id { get; init; }

    public string IPAddress { get; init; } = string.Empty;
    public DateTime LastConnectionTime { get; init; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<UserIP> UserIPs { get; init; } = new HashSet<UserIP>();
}