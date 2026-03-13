namespace Whycespace.EventFabricRuntime.Tests;

using Whycespace.EventFabricRuntime.Models;
using Whycespace.EventFabricRuntime.Publishing;
using Whycespace.EventFabricRuntime.Routing;

public class EventPublisherTests
{
    [Fact]
    public void Publish_SerializesAndRoutesToTopic()
    {
        var serializer = new EventSerializer();
        var router = new EventTopicRouter();
        router.Register("CapitalContributionRecorded", "whyce.capital.events");

        var publisher = new EventPublisher(serializer, router);
        var envelope = new EventEnvelope("evt-1", "CapitalContributionRecorded", new { Amount = 100 });

        var result = publisher.Publish(envelope);

        Assert.StartsWith("whyce.capital.events:", result);
        Assert.Contains("CapitalContributionRecorded", result);
    }

    [Fact]
    public void Publish_UnregisteredEvent_Throws()
    {
        var serializer = new EventSerializer();
        var router = new EventTopicRouter();
        var publisher = new EventPublisher(serializer, router);

        var envelope = new EventEnvelope("evt-1", "UnknownEvent", new { });

        Assert.Throws<InvalidOperationException>(() => publisher.Publish(envelope));
    }

    [Fact]
    public void Serializer_ProducesValidJson()
    {
        var serializer = new EventSerializer();
        var envelope = new EventEnvelope("evt-1", "TestEvent", new { Key = "value" });

        var json = serializer.Serialize(envelope);

        Assert.Contains("\"EventId\"", json);
        Assert.Contains("\"EventType\"", json);
        Assert.Contains("evt-1", json);
        Assert.Contains("TestEvent", json);
    }
}
