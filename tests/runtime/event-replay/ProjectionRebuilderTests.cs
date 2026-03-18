using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Router;
using Whycespace.EventFabric.Topics;
using Whycespace.EventReplay.Engine;
using Whycespace.EventReplay.Projections;

namespace Whycespace.EventReplay.Tests;

public class ProjectionRebuilderTests
{
    [Fact]
    public async Task RebuildAsync_Resets_And_Replays()
    {
        var wasReset = false;
        var routed = new List<EventEnvelope>();

        var router = new EventRouter();
        router.Register(EventTopics.EngineEvents, e => { routed.Add(e); return Task.CompletedTask; });

        var engine = new EventReplayEngine(router);
        var rebuilder = new ProjectionRebuilder(engine);

        var events = new List<EventEnvelope>
        {
            new(Guid.NewGuid(), "E1", EventTopics.EngineEvents, new { },
                new PartitionKey("pk-1"), Timestamp.Now())
        };

        rebuilder.RegisterProjection(
            "TestProjection",
            () => wasReset = true,
            () => events
        );

        await rebuilder.RebuildAsync("TestProjection");

        Assert.True(wasReset);
        Assert.Single(routed);
    }

    [Fact]
    public async Task RebuildAsync_Throws_For_Unregistered_Projection()
    {
        var router = new EventRouter();
        var engine = new EventReplayEngine(router);
        var rebuilder = new ProjectionRebuilder(engine);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => rebuilder.RebuildAsync("Unknown"));
    }

    [Fact]
    public void GetRegisteredProjections_Returns_Names()
    {
        var router = new EventRouter();
        var engine = new EventReplayEngine(router);
        var rebuilder = new ProjectionRebuilder(engine);

        rebuilder.RegisterProjection("P1", () => { }, () => []);
        rebuilder.RegisterProjection("P2", () => { }, () => []);

        var names = rebuilder.GetRegisteredProjections();
        Assert.Equal(2, names.Count);
        Assert.Contains("P1", names);
        Assert.Contains("P2", names);
    }
}
