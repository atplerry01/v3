using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventIdempotency.Guard;
using Whycespace.EventIdempotency.Registry;
using Whycespace.Projections.Consumers;
using Whycespace.Projections.Engine;
using Whycespace.Projections.Registry;

namespace Whycespace.Projections.Tests;

public sealed class ProjectionConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_ProcessesEvent()
    {
        var registry = new ProjectionRegistry();
        var projection = new CountingProjection("Counter", ["TestEvent"]);
        registry.Register(projection);

        var engine = new ProjectionEngine(registry);
        var guard = new EventProcessingGuard(new EventDeduplicationRegistry());
        var consumer = new ProjectionEventConsumer(engine, guard);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "TestEvent",
            "whyce.workflow.events",
            new Dictionary<string, object>(),
            new PartitionKey("key-1"),
            Timestamp.Now());

        await consumer.ConsumeAsync(envelope);

        Assert.Equal(1, projection.HandleCount);
    }

    [Fact]
    public async Task ConsumeAsync_DeduplicatesSameEvent()
    {
        var registry = new ProjectionRegistry();
        var projection = new CountingProjection("Counter", ["TestEvent"]);
        registry.Register(projection);

        var engine = new ProjectionEngine(registry);
        var guard = new EventProcessingGuard(new EventDeduplicationRegistry());
        var consumer = new ProjectionEventConsumer(engine, guard);

        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelope(
            eventId,
            "TestEvent",
            "whyce.workflow.events",
            new Dictionary<string, object>(),
            new PartitionKey("key-1"),
            Timestamp.Now());

        await consumer.ConsumeAsync(envelope);
        await consumer.ConsumeAsync(envelope);

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
