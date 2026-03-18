
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;
using Whycespace.ProjectionRuntime.Storage;
using Whycespace.ProjectionRebuild.Checkpoints;
using Whycespace.ProjectionRebuild.Controller;
using Whycespace.ProjectionRebuild.Reader;
using Whycespace.ProjectionRebuild.Rebuild;
using Whycespace.ProjectionRebuild.Reset;

namespace Whycespace.ProjectionRebuild.Tests;

public class ProjectionReplayControllerTests
{
    private class StubProjection : IProjection
    {
        public string Name => "stub";
        public IReadOnlyCollection<string> EventTypes => new[] { "TestEvent" };
        public int HandleCount { get; private set; }
        public Task HandleAsync(EventEnvelope envelope)
        {
            HandleCount++;
            return Task.CompletedTask;
        }
    }

    private static EventEnvelope CreateEnvelope()
    {
        return new EventEnvelope(
            Guid.NewGuid(),
            "TestEvent",
            "test-topic",
            new { },
            new PartitionKey("test"),
            new Timestamp(DateTime.UtcNow));
    }

    [Fact]
    public async Task RebuildAllAsync_ResetsAndRebuilds()
    {
        var reader = new EventLogReader();
        reader.Append(CreateEnvelope());
        reader.Append(CreateEnvelope());

        var stub = new StubProjection();
        var registry = new ProjectionRegistry();
        registry.Register(stub);

        var store = new RedisProjectionStore();
        var resetService = new ProjectionResetService(store, registry);
        var checkpointStore = new ProjectionCheckpointStore();
        var rebuildEngine = new ProjectionRebuildEngine(reader, registry, resetService, checkpointStore);

        var controller = new ProjectionReplayController(rebuildEngine, resetService, registry);
        await controller.RebuildAllAsync();

        Assert.Equal(2, stub.HandleCount);
        var status = controller.GetStatus();
        Assert.False(status.Rebuilding);
        Assert.Equal(2, status.ProcessedEvents);
    }

    [Fact]
    public async Task RebuildProjectionAsync_RebuildsSpecificProjection()
    {
        var reader = new EventLogReader();
        reader.Append(CreateEnvelope());

        var stub = new StubProjection();
        var registry = new ProjectionRegistry();
        registry.Register(stub);

        var store = new RedisProjectionStore();
        var resetService = new ProjectionResetService(store, registry);
        var checkpointStore = new ProjectionCheckpointStore();
        var rebuildEngine = new ProjectionRebuildEngine(reader, registry, resetService, checkpointStore);

        var controller = new ProjectionReplayController(rebuildEngine, resetService, registry);
        await controller.RebuildProjectionAsync("stub");

        Assert.Equal(1, stub.HandleCount);
    }
}
