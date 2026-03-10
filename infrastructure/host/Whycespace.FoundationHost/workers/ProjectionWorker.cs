namespace Whycespace.FoundationHost.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Events;
using Whycespace.Shared.Projections;

public sealed class ProjectionWorker : BackgroundService
{
    private readonly EventBus _eventBus;
    private readonly IReadOnlyList<IProjection> _projections;
    private readonly ILogger<ProjectionWorker> _logger;

    public ProjectionWorker(
        EventBus eventBus,
        IReadOnlyList<IProjection> projections,
        ILogger<ProjectionWorker> logger)
    {
        _eventBus = eventBus;
        _projections = projections;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectionWorker started — subscribing {Count} projections to EventBus",
            _projections.Count);

        foreach (var projection in _projections)
        {
            _eventBus.Subscribe("*", async @event =>
            {
                await projection.HandleAsync(@event);
            });

            _logger.LogDebug("Subscribed projection {Name}", projection.Name);
        }

        return Task.CompletedTask;
    }
}
