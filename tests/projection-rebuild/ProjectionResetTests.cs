using Whycespace.Projections.Engine;
using Whycespace.Projections.Registry;
using Whycespace.Projections.Storage;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRebuild.Reset;

namespace Whycespace.ProjectionRebuild.Tests;

public class ProjectionResetTests
{
    private class StubProjection : IProjection
    {
        public string Name => "stub";
        public IReadOnlyCollection<string> EventTypes => new[] { "TestEvent" };
        public Task HandleAsync(EventEnvelope envelope) => Task.CompletedTask;
    }

    [Fact]
    public async Task ResetAsync_DeletesProjectionKey()
    {
        var store = new RedisProjectionStore();
        await store.SetAsync("projection:stub", "data");

        var registry = new ProjectionRegistry();
        registry.Register(new StubProjection());

        var resetService = new ProjectionResetService(store, registry);
        await resetService.ResetAsync("stub");

        var result = await store.GetAsync("projection:stub");
        Assert.Null(result);
    }

    [Fact]
    public async Task ResetAllAsync_DeletesAllProjectionKeys()
    {
        var store = new RedisProjectionStore();
        await store.SetAsync("projection:stub", "data");

        var registry = new ProjectionRegistry();
        registry.Register(new StubProjection());

        var resetService = new ProjectionResetService(store, registry);
        await resetService.ResetAllAsync();

        var result = await store.GetAsync("projection:stub");
        Assert.Null(result);
    }
}
