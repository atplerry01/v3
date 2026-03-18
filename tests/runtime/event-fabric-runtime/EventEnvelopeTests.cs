
namespace Whycespace.EventFabricRuntime.Tests;

using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;
using Whycespace.Shared.Primitives.Common;

public class EventEnvelopeTests
{
    [Fact]
    public void Envelope_StoresEventId()
    {
        var id = Guid.NewGuid();
        var envelope = new EventEnvelope(id, "TestEvent", "", new { Data = "test" }, new PartitionKey("default"), Timestamp.Now());
        Assert.Equal(id, envelope.EventId);
    }

    [Fact]
    public void Envelope_StoresEventType()
    {
        var envelope = new EventEnvelope(Guid.NewGuid(), "TestEvent", "", new { Data = "test" }, new PartitionKey("default"), Timestamp.Now());
        Assert.Equal("TestEvent", envelope.EventType);
    }

    [Fact]
    public void Envelope_StoresPayload()
    {
        var payload = new { Data = "test" };
        var envelope = new EventEnvelope(Guid.NewGuid(), "TestEvent", "", payload, new PartitionKey("default"), Timestamp.Now());
        Assert.Equal(payload, envelope.Payload);
    }

    [Fact]
    public void Envelope_AssignsTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var envelope = new EventEnvelope(Guid.NewGuid(), "TestEvent", "", new { }, new PartitionKey("default"), Timestamp.Now());
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(envelope.Timestamp.Value, before, after);
    }
}
