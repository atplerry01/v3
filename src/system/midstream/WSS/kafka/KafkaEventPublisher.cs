namespace Whycespace.System.Midstream.WSS.Kafka;

using global::System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Events;

public sealed class KafkaEventPublisher : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IProducer<string, string>? _producer;
    private readonly ILogger<KafkaEventPublisher>? _logger;

    public KafkaEventPublisher(IEventBus eventBus, string bootstrapServers = "localhost:9092", ILogger<KafkaEventPublisher>? logger = null)
    {
        _eventBus = eventBus;
        _logger = logger;

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000,
                EnableIdempotence = true
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Kafka producer unavailable — events will only publish to in-memory EventBus");
        }
    }

    public async Task PublishToTopicAsync(string topic, SystemEvent @event)
    {
        await _eventBus.PublishAsync(@event);

        if (_producer is null)
            return;

        try
        {
            var key = @event.AggregateId.ToString();
            var value = JsonSerializer.Serialize(new
            {
                @event.EventId,
                @event.EventType,
                @event.AggregateId,
                @event.Timestamp,
                @event.Payload
            });

            var message = new Message<string, string> { Key = key, Value = value };
            var result = await _producer.ProduceAsync(topic, message);

            _logger?.LogDebug("Published {EventType} to {Topic} partition {Partition} offset {Offset}",
                @event.EventType, topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger?.LogError(ex, "Failed to publish {EventType} to Kafka topic {Topic}", @event.EventType, topic);
        }
    }

    public async Task PublishEngineEventAsync(string topic, Whycespace.Contracts.Engines.EngineEvent engineEvent)
    {
        var systemEvent = SystemEvent.Create(engineEvent.EventType, engineEvent.AggregateId, engineEvent.Payload);
        await PublishToTopicAsync(topic, systemEvent);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}
