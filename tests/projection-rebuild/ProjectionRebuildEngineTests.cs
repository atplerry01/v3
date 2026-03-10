using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Engine;
using Whycespace.Projections.Registry;
using Whycespace.Projections.Storage;
using Whycespace.ProjectionRebuild.Checkpoints;
using Whycespace.ProjectionRebuild.Reader;
using Whycespace.ProjectionRebuild.Rebuild;
using Whycespace.ProjectionRebuild.Reset;

namespace Whycespace.ProjectionRebuild.Tests;

public class ProjectionRebuildEngineTests
{
    private class CountingProjection : IProjection
    {
        public string Name => "counter";
        public IReadOnlyCollection<string> EventTypes => new[] { "TestEvent" };
        public int HandleCount { get; private set; }
        public Task HandleAsync(EventEnvelope envelope)
        {
            HandleCount++;
            return Task.CompletedTask;
        }
    }

    private static EventEnvelope CreateEnvelope(string eventType = "TestEvent")
    {
        return new EventEnvelope(
            Guid.NewGuid(),
            eventType,
            "test-topic",
            new { },
            new PartitionKey("test"),
            new Timestamp(DateTime.UtcNow));
    }

    [Fact]
    public async Task RebuildAsync_ProcessesAllEvents()
    {
        var reader = new EventLogReader();
        reader.Append(CreateEnvelope());
        reader.Append(CreateEnvelope());
        reader.Append(CreateEnvelope());

        var counting = new CountingProjection();
        var registry = new ProjectionRegistry();
        registry.Register(counting);

        var projectionEngine = new ProjectionEngine(registry);
        var store = new RedisProjectionStore();
        var resetService = new ProjectionResetService(store, registry);
        var checkpointStore = new ProjectionCheckpointStore();

        var rebuildEngine = new ProjectionRebuildEngine(reader, projectionEngine, resetService, checkpointStore);
        await rebuildEngine.RebuildAsync();

        Assert.Equal(3, counting.HandleCount);
        Assert.Equal(3, rebuildEngine.Status.ProcessedEvents);
        Assert.False(rebuildEngine.Status.Rebuilding);
        Assert.NotNull(rebuildEngine.Status.CompletedAt);
    }

    [Fact]
    public async Task RebuildProjectionAsync_SavesCheckpoint()
    {
        var reader = new EventLogReader();
        reader.Append(CreateEnvelope());
        reader.Append(CreateEnvelope());

        var counting = new CountingProjection();
        var registry = new ProjectionRegistry();
        registry.Register(counting);

        var projectionEngine = new ProjectionEngine(registry);
        var store = new RedisProjectionStore();
        var resetService = new ProjectionResetService(store, registry);
        var checkpointStore = new ProjectionCheckpointStore();

        var rebuildEngine = new ProjectionRebuildEngine(reader, projectionEngine, resetService, checkpointStore);
        await rebuildEngine.RebuildProjectionAsync("counter");

        var checkpoint = await checkpointStore.LoadCheckpointAsync("counter");
        Assert.NotNull(checkpoint);
        Assert.Equal("counter", checkpoint!.ProjectionName);
    }
}
