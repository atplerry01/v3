namespace Whycespace.EventFabricRuntime.Tests;

using Whycespace.EventFabricRuntime.Models;

public class EventEnvelopeTests
{
    [Fact]
    public void Envelope_StoresEventId()
    {
        var envelope = new EventEnvelope("evt-1", "TestEvent", new { Data = "test" });
        Assert.Equal("evt-1", envelope.EventId);
    }

    [Fact]
    public void Envelope_StoresEventType()
    {
        var envelope = new EventEnvelope("evt-1", "TestEvent", new { Data = "test" });
        Assert.Equal("TestEvent", envelope.EventType);
    }

    [Fact]
    public void Envelope_StoresPayload()
    {
        var payload = new { Data = "test" };
        var envelope = new EventEnvelope("evt-1", "TestEvent", payload);
        Assert.Equal(payload, envelope.Payload);
    }

    [Fact]
    public void Envelope_AssignsTimestamp()
    {
        var before = DateTime.UtcNow;
        var envelope = new EventEnvelope("evt-1", "TestEvent", new { });
        var after = DateTime.UtcNow;

        Assert.InRange(envelope.TimestampUtc, before, after);
    }
}
