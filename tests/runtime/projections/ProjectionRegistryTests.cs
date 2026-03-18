
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;

namespace Whycespace.ProjectionRuntime.Projections.Tests;

public sealed class ProjectionRegistryTests
{
    [Fact]
    public void Register_Projection_ResolvesCorrectly()
    {
        var registry = new ProjectionRegistry();
        var projection = new StubProjection("Test", ["OrderCreated"]);

        registry.Register(projection);

        var resolved = registry.Resolve("OrderCreated");
        Assert.Single(resolved);
        Assert.Equal("Test", resolved.First().Name);
    }

    [Fact]
    public void Resolve_UnknownEventType_ReturnsEmpty()
    {
        var registry = new ProjectionRegistry();

        var resolved = registry.Resolve("UnknownEvent");

        Assert.Empty(resolved);
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        var registry = new ProjectionRegistry();
        registry.Register(new StubProjection("A", ["E1"]));
        registry.Register(new StubProjection("B", ["E2"]));

        var all = registry.GetAll();

        Assert.Equal(2, all.Count);
    }

    private sealed class StubProjection : IProjection
    {
        public StubProjection(string name, string[] eventTypes)
        {
            Name = name;
            EventTypes = eventTypes;
        }

        public string Name { get; }
        public IReadOnlyCollection<string> EventTypes { get; }

        public Task HandleAsync(EventEnvelope envelope) => Task.CompletedTask;
    }
}
