namespace IpLogsCommon.Repository.Entities;

public class UserIP
{
    public long Id { get; init; }
    public long UserId { get; init; }

    // ReSharper disable once InconsistentNaming
    public string IPAddress { get; init; } = string.Empty;
    public DateTime ConnectionTime { get; init; }

    public User? User { get; init; }
}