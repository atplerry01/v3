using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Router;
using Whycespace.EventFabric.Topics;

namespace Whycespace.EventFabric.Tests;

public class EventRouterTests
{
    [Fact]
    public async Task RouteAsync_Dispatches_To_Registered_Handler()
    {
        var router = new EventRouter();
        var handled = new List<EventEnvelope>();

        router.Register(EventTopics.EngineEvents, e =>
        {
            handled.Add(e);
            return Task.CompletedTask;
        });

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "DriverMatchedEvent",
            EventTopics.EngineEvents,
            new { DriverId = "d-1" },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        await router.RouteAsync(envelope);

        Assert.Single(handled);
        Assert.Equal("DriverMatchedEvent", handled[0].EventType);
    }

    [Fact]
    public async Task RouteAsync_NoHandler_Does_Not_Throw()
    {
        var router = new EventRouter();

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "UnhandledEvent",
            EventTopics.SystemEvents,
            new { },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        await router.RouteAsync(envelope);
    }

    [Fact]
    public async Task RouteAsync_Multiple_Handlers_All_Invoked()
    {
        var router = new EventRouter();
        var count = 0;

        router.Register(EventTopics.WorkflowEvents, _ => { count++; return Task.CompletedTask; });
        router.Register(EventTopics.WorkflowEvents, _ => { count++; return Task.CompletedTask; });

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "WorkflowCompleted",
            EventTopics.WorkflowEvents,
            new { },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        await router.RouteAsync(envelope);

        Assert.Equal(2, count);
    }
}
