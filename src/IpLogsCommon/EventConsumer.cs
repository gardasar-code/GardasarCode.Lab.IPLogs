using System.Text.Json;
using Confluent.Kafka;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;
using IpLogsCommon.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IpLogsCommon;

public class EventConsumer : IEventConsumer
{
    private readonly IIPLogsService _ipLogsService;
    private readonly IConsumer<Ignore, string> _kafkaConsumer;
    private readonly ILogger<EventConsumer> _logger;
    private bool _disposed;

    public EventConsumer(IOptionsSnapshot<KafkaOptions> options, IIPLogsService ipLogsService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EventConsumer>();
        _ipLogsService = ipLogsService;

        var config = new ConsumerConfig
        {
            GroupId = options.Value.ConsumerGroup,
            BootstrapServers = options.Value.Broker,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _kafkaConsumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _kafkaConsumer.Subscribe(options.Value.Topic);
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var consumeResult = _kafkaConsumer.Consume(cancellationToken);
            if (consumeResult == null) return;

            _logger.LogDebug($"Received message: {consumeResult.Message.Value}");
            var eventMessage = JsonSerializer.Deserialize<EventMessage>(consumeResult.Message.Value);
            if (eventMessage != null)
                await ProcessEventAsync(eventMessage, cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError($"JSON deserialization error: {e.Message}");
            throw;
        }
        catch (ConsumeException e)
        {
            _logger.LogError($"Consume error: {e.Error.Reason}");
        }
        catch (KafkaException e)
        {
            _logger.LogError($"Kafka error: {e.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error: {e.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _kafkaConsumer?.Dispose();
        _disposed = true;
    }

    private async Task ProcessEventAsync(EventMessage eventMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            $"Processed Event: UserID: {eventMessage.UserId}, IP: {eventMessage.IpAddress}, Time: {eventMessage.EventTime}");

        await _ipLogsService.AddConnectionAsync(eventMessage.UserId, eventMessage.IpAddress, eventMessage.EventTime,
            cancellationToken);
    }
}