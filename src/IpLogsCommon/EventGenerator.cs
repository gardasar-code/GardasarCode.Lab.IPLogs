using System.Net;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;

namespace IpLogsCommon;

public class EventGenerator : IEventGenerator
{
    private readonly List<string> _ipAddresses = [];
    private readonly Random _random = new();

    public EventGenerator()
    {
        for (var i = 0; i < 10; i++)
        {
            _ipAddresses.Add(GenerateRandomIPv4());
            _ipAddresses.Add(GenerateRandomIPv6());
        }
    }

    public EventMessage Generate()
    {
        var userId = _random.Next(1, 11);
        var ipAddress = _ipAddresses[_random.Next(0, _ipAddresses.Count)];
        var eventTime = DateTime.Now;

        var eventMessage = new EventMessage
        {
            UserId = userId,
            IpAddress = ipAddress,
            EventTime = eventTime
        };

        return eventMessage;
    }

    private string GenerateRandomIPv4()
    {
        return string.Join(".", _random.Next(0, 256), _random.Next(0, 256), _random.Next(0, 256), _random.Next(0, 256));
    }

    private string GenerateRandomIPv6()
    {
        var address = new byte[16];
        _random.NextBytes(address);
        return new IPAddress(address).ToString();
    }
}