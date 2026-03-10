using System.Text.Json;
using Confluent.Kafka;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Router;

namespace Whycespace.EventFabric.Consumer;

public sealed class KafkaEventConsumer : IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly EventRouter _router;
    private readonly IReadOnlyList<string> _topics;

    public KafkaEventConsumer(
        string bootstrapServers,
        string groupId,
        IReadOnlyList<string> topics,
        EventRouter router)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _router = router;
        _topics = topics;
    }

    public KafkaEventConsumer(
        IConsumer<string, string> consumer,
        IReadOnlyList<string> topics,
        EventRouter router)
    {
        _consumer = consumer;
        _router = router;
        _topics = topics;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.Subscribe(_topics);

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = _consumer.Consume(cancellationToken);

            if (result?.Message?.Value is null)
                continue;

            var envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Message.Value);

            if (envelope is not null)
            {
                await _router.RouteAsync(envelope);
            }
        }
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}
