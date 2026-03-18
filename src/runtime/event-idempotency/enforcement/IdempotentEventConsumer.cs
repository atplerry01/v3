
using System.Text.Json;
using Whycespace.Shared.Envelopes;
using Confluent.Kafka;
using Whycespace.Contracts.Events;
using Whycespace.EventFabric.Router;
using Whycespace.EventIdempotency.Guard;

namespace Whycespace.EventIdempotency.Enforcement;

public sealed class IdempotentEventConsumer : IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly EventRouter _router;
    private readonly EventProcessingGuard _guard;
    private readonly IReadOnlyList<string> _topics;

    public IdempotentEventConsumer(
        IConsumer<string, string> consumer,
        IReadOnlyList<string> topics,
        EventRouter router,
        EventProcessingGuard guard)
    {
        _consumer = consumer;
        _router = router;
        _guard = guard;
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

            if (envelope is null)
                continue;

            if (!_guard.ShouldProcess(envelope))
                continue;

            await _router.RouteAsync(envelope);
        }
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}
