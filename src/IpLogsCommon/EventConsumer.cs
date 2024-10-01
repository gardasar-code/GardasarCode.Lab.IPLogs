using System.Text.Json;
using Confluent.Kafka;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;
using IpLogsCommon.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IpLogsCommon;

public sealed class EventConsumer : IEventConsumer
{
    private readonly IIPLogsService _ipLogsService;
    private readonly IConsumer<Ignore, string> _kafkaConsumer;
    private readonly ILogger<EventConsumer> _logger;

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

            _logger.LogDebug("Received message: {value}", consumeResult.Message.Value);
            var eventMessage = JsonSerializer.Deserialize<EventMessage>(consumeResult.Message.Value);
            if (eventMessage != null)
                await ProcessEventAsync(eventMessage, cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError("JSON deserialization error: {message}", e.Message);
            throw;
        }
        catch (ConsumeException e)
        {
            _logger.LogError("Consume error: {reason}", e.Error.Reason);
        }
        catch (KafkaException e)
        {
            _logger.LogError("Kafka error: {message}", e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError("Unexpected error: {message}", e.Message);
            throw;
        }
    }

    private async Task ProcessEventAsync(EventMessage eventMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Processed Event: UserID: {UserId}, IP: {IpAddress}, Time: {EventTime}", eventMessage.UserId,
            eventMessage.IpAddress, eventMessage.EventTime);

        await _ipLogsService.AddConnectionAsync(eventMessage.UserId, eventMessage.IpAddress, eventMessage.EventTime,
            cancellationToken);
    }

    #region Dispose

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~EventConsumer()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _kafkaConsumer.Dispose();
        _disposed = true;
    }

    #endregion
}