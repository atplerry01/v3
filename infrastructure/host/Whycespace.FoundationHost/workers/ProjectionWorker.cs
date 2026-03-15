namespace Whycespace.FoundationHost.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventIdempotency.Guard;
using Whycespace.Projections.Registry;
using Whycespace.Runtime.Events;

public sealed class ProjectionWorker : BackgroundService
{
    private readonly EventBus _eventBus;
    private readonly IProjectionRegistry _registry;
    private readonly EventProcessingGuard _guard;
    private readonly ILogger<ProjectionWorker> _logger;

    public ProjectionWorker(
        EventBus eventBus,
        IProjectionRegistry registry,
        EventProcessingGuard guard,
        ILogger<ProjectionWorker> logger)
    {
        _eventBus = eventBus;
        _registry = registry;
        _guard = guard;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectionWorker started — forwarding EventBus events to projection registry");

        _eventBus.Subscribe("*", async @event =>
        {
            var envelope = new EventEnvelope(
                @event.EventId,
                @event.EventType,
                "whyce.events",
                @event.Payload,
                new PartitionKey(@event.AggregateId.ToString()),
                new Timestamp(@event.Timestamp));

            if (!_guard.ShouldProcess(envelope))
                return;

            var projections = _registry.Resolve(envelope.EventType);

            foreach (var projection in projections)
            {
                await projection.HandleAsync(envelope);
            }
        });

        return Task.CompletedTask;
    }
}
