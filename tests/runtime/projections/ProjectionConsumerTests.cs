
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EventIdempotency.Guard;
using Whycespace.EventIdempotency.Registry;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;

namespace Whycespace.ProjectionRuntime.Projections.Tests;

public sealed class ProjectionConsumerTests
{
    [Fact]
    public async Task RegistryResolveWithGuard_ProcessesEvent()
    {
        var registry = new ProjectionRegistry();
        var projection = new CountingProjection("Counter", ["TestEvent"]);
        registry.Register(projection);

        var guard = new EventProcessingGuard(new EventDeduplicationRegistry());

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "TestEvent",
            "whyce.workflow.events",
            new Dictionary<string, object>(),
            new PartitionKey("key-1"),
            Timestamp.Now());

        if (guard.ShouldProcess(envelope))
        {
            var projections = registry.Resolve(envelope.EventType);
            foreach (var p in projections)
                await p.HandleAsync(envelope);
        }

        Assert.Equal(1, projection.HandleCount);
    }

    [Fact]
    public async Task RegistryResolveWithGuard_DeduplicatesSameEvent()
    {
        var registry = new ProjectionRegistry();
        var projection = new CountingProjection("Counter", ["TestEvent"]);
        registry.Register(projection);

        var guard = new EventProcessingGuard(new EventDeduplicationRegistry());

        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelope(
            eventId,
            "TestEvent",
            "whyce.workflow.events",
            new Dictionary<string, object>(),
            new PartitionKey("key-1"),
            Timestamp.Now());

        for (var i = 0; i < 2; i++)
        {
            if (guard.ShouldProcess(envelope))
            {
                var projections = registry.Resolve(envelope.EventType);
                foreach (var p in projections)
                    await p.HandleAsync(envelope);
            }
        }

        Assert.Equal(1, projection.HandleCount);
    }

    private sealed class CountingProjection : IProjection
    {
        public CountingProjection(string name, string[] eventTypes)
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
