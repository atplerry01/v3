
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;
using Whycespace.ProjectionRuntime.Storage;
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

        var store = new RedisProjectionStore();
        var resetService = new ProjectionResetService(store, registry);
        var checkpointStore = new ProjectionCheckpointStore();

        var rebuildEngine = new ProjectionRebuildEngine(reader, registry, resetService, checkpointStore);
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

        var store = new RedisProjectionStore();
        var resetService = new ProjectionResetService(store, registry);
        var checkpointStore = new ProjectionCheckpointStore();

        var rebuildEngine = new ProjectionRebuildEngine(reader, registry, resetService, checkpointStore);
        await rebuildEngine.RebuildProjectionAsync("counter");

        var checkpoint = await checkpointStore.LoadCheckpointAsync("counter");
        Assert.NotNull(checkpoint);
        Assert.Equal("counter", checkpoint!.ProjectionName);
    }
}
