
namespace Whycespace.EventFabricRuntime.Tests;

using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;
using Whycespace.Shared.Primitives.Common;
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
        var envelope = new EventEnvelope(Guid.NewGuid(), "CapitalContributionRecorded", "", new { Amount = 100 }, new PartitionKey("default"), Timestamp.Now());

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

        var envelope = new EventEnvelope(Guid.NewGuid(), "UnknownEvent", "", new { }, new PartitionKey("default"), Timestamp.Now());

        Assert.Throws<InvalidOperationException>(() => publisher.Publish(envelope));
    }

    [Fact]
    public void Serializer_ProducesValidJson()
    {
        var serializer = new EventSerializer();
        var id = Guid.NewGuid();
        var envelope = new EventEnvelope(id, "TestEvent", "", new { Key = "value" }, new PartitionKey("default"), Timestamp.Now());

        var json = serializer.Serialize(envelope);

        Assert.Contains("\"EventId\"", json);
        Assert.Contains("\"EventType\"", json);
        Assert.Contains(id.ToString(), json);
        Assert.Contains("TestEvent", json);
    }
}
