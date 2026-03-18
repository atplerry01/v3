namespace Whycespace.EventFabricRuntime.Tests;

using Whycespace.EventFabricRuntime.Consuming;
using Whycespace.EventFabricRuntime.Registry;

public class EventSubscriptionRegistryTests
{
    [Fact]
    public void Subscribe_And_GetSubscribers()
    {
        var registry = new EventSubscriptionRegistry();
        registry.Subscribe("whyce.capital.events", "ProjectionService");

        var subscribers = registry.GetSubscribers("whyce.capital.events");
        Assert.Single(subscribers);
        Assert.Equal("ProjectionService", subscribers[0]);
    }

    [Fact]
    public void GetSubscribers_UnknownTopic_ReturnsEmpty()
    {
        var registry = new EventSubscriptionRegistry();
        var subscribers = registry.GetSubscribers("unknown.topic");

        Assert.Empty(subscribers);
    }

    [Fact]
    public void Subscribe_MultipleSubscribers_SameTopic()
    {
        var registry = new EventSubscriptionRegistry();
        registry.Subscribe("whyce.capital.events", "ServiceA");
        registry.Subscribe("whyce.capital.events", "ServiceB");

        var subscribers = registry.GetSubscribers("whyce.capital.events");
        Assert.Equal(2, subscribers.Count);
    }

    [Fact]
    public void GetSubscriptionCounts_ReturnsCorrectCounts()
    {
        var registry = new EventSubscriptionRegistry();
        registry.Subscribe("topic-1", "A");
        registry.Subscribe("topic-1", "B");
        registry.Subscribe("topic-2", "C");

        var counts = registry.GetSubscriptionCounts();
        Assert.Equal(2, counts["topic-1"]);
        Assert.Equal(1, counts["topic-2"]);
    }

    [Fact]
    public void ConsumerRuntime_DelegatesToRegistry()
    {
        var registry = new EventSubscriptionRegistry();
        registry.Subscribe("whyce.engine.events", "Handler1");

        var consumer = new EventConsumerRuntime(registry);
        var subscribers = consumer.GetSubscribers("whyce.engine.events");

        Assert.Single(subscribers);
        Assert.Equal("Handler1", subscribers[0]);
    }
}
