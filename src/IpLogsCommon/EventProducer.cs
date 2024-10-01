using System.Text.Json;
using Confluent.Kafka;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;
using IpLogsCommon.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IpLogsCommon;

public sealed class EventProducer : IEventProducer
{
    private readonly IProducer<Null, string> _kafkaProducer;
    private readonly ILogger<EventProducer> _logger;
    private readonly IOptionsSnapshot<KafkaOptions> _options;

    public EventProducer(IOptionsSnapshot<KafkaOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<EventProducer>();

        var config = new ProducerConfig
        {
            BootstrapServers = _options.Value.Broker
        };

        _kafkaProducer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceAsync(EventMessage eventMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _kafkaProducer.ProduceAsync(_options.Value.Topic,
                new Message<Null, string> { Value = JsonSerializer.Serialize(eventMessage) }, cancellationToken)!;

            _logger.LogDebug(
                "Processed Event: UserID: {UserId}, IP: {IpAddress}, Time: {EventTime}, Message sent to topic: {TopicPartitionOffset}",
                eventMessage.UserId, eventMessage.IpAddress, eventMessage.EventTime, report.TopicPartitionOffset);
        }
        catch (ProduceException<Null, string> e)
        {
            if (e.Error.IsError)
                _logger.LogError("Error sending message: {ereason}", e.Error.Reason);
        }
    }

    #region Dispose

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _kafkaProducer.Dispose();
        _disposed = true;
    }

    ~EventProducer()
    {
        Dispose(false);
    }

    #endregion
}