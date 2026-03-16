using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;

namespace Whycespace.ProjectionRuntime.Projections.Tests;

public sealed class ProjectionEngineTests
{
    [Fact]
    public async Task Registry_ResolveAndProcess_DispatchesToMatchingProjection()
    {
        var registry = new ProjectionRegistry();
        var projection = new TrackingProjection("Tracker", ["OrderCreated"]);
        registry.Register(projection);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "OrderCreated",
            "whyce.workflow.events",
            new Dictionary<string, object>(),
            new PartitionKey("order-1"),
            Timestamp.Now());

        var resolved = registry.Resolve(envelope.EventType);
        foreach (var p in resolved)
            await p.HandleAsync(envelope);

        Assert.Equal(1, projection.HandleCount);
    }

    [Fact]
    public async Task Registry_ResolveAndProcess_IgnoresNonMatchingProjection()
    {
        var registry = new ProjectionRegistry();
        var projection = new TrackingProjection("Tracker", ["OrderCreated"]);
        registry.Register(projection);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "SomethingElse",
            "whyce.workflow.events",
            new Dictionary<string, object>(),
            new PartitionKey("x"),
            Timestamp.Now());

        var resolved = registry.Resolve(envelope.EventType);
        foreach (var p in resolved)
            await p.HandleAsync(envelope);

        Assert.Equal(0, projection.HandleCount);
    }

    private sealed class TrackingProjection : IProjection
    {
        public TrackingProjection(string name, string[] eventTypes)
        {
            Name = name;
            EventTypes = eventTypes;
        }

        public string Name { get; }
        public IReadOnlyCollection<string> EventTypes { get; }
        public int HandleCount { get; private set; }

        public Task HandleAsync(EventEnvelope envelope)
        {
            HandleCount++;
            return Task.CompletedTask;
        }
    }
}
