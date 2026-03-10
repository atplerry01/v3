using System.Text.Json;
using Confluent.Kafka;
using Whycespace.EventFabric.Models;

namespace Whycespace.EventFabric.Publisher;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public KafkaEventPublisher(IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(
        string topic,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(envelope);

        var message = new Message<string, string>
        {
            Key = envelope.PartitionKey.Value,
            Value = json
        };

        await _producer.ProduceAsync(topic, message, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
