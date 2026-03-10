namespace Whycespace.FoundationHost.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Consumers;
using Whycespace.Runtime.Events;

public sealed class ProjectionWorker : BackgroundService
{
    private readonly EventBus _eventBus;
    private readonly ProjectionEventConsumer _consumer;
    private readonly ILogger<ProjectionWorker> _logger;

    public ProjectionWorker(
        EventBus eventBus,
        ProjectionEventConsumer consumer,
        ILogger<ProjectionWorker> logger)
    {
        _eventBus = eventBus;
        _consumer = consumer;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectionWorker started — forwarding EventBus events to ProjectionEventConsumer");

        _eventBus.Subscribe("*", async @event =>
        {
            var envelope = new EventEnvelope(
                @event.EventId,
                @event.EventType,
                "whyce.events",
                @event.Payload,
                new PartitionKey(@event.AggregateId.ToString()),
                new Timestamp(@event.Timestamp));

            await _consumer.ConsumeAsync(envelope);
        });

        return Task.CompletedTask;
    }
}
