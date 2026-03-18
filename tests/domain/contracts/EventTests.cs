namespace Whycespace.Contracts.Tests;

using Whycespace.Contracts.Events;

public sealed class EventTests
{
    [Fact]
    public void SystemEvent_Create_Generates_Valid_Event()
    {
        var aggregateId = Guid.NewGuid();
        var @event = SystemEvent.Create("TestEvent", aggregateId);

        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.Equal("TestEvent", @event.EventType);
        Assert.Equal(aggregateId, @event.AggregateId);
        Assert.True(@event.Timestamp <= DateTimeOffset.UtcNow);
        Assert.NotNull(@event.Payload);
    }

    [Fact]
    public void SystemEvent_Implements_IEvent()
    {
        var @event = SystemEvent.Create("Test", Guid.NewGuid());
        IEvent ievent = @event;

        Assert.Equal(@event.EventId, ievent.EventId);
        Assert.Equal(@event.EventType, ievent.EventType);
        Assert.Equal(@event.AggregateId, ievent.AggregateId);
    }

    [Fact]
    public void SystemEvent_Inherits_EventBase()
    {
        var @event = SystemEvent.Create("Test", Guid.NewGuid());
        EventBase baseEvent = @event;

        Assert.Equal(@event.EventId, baseEvent.EventId);
    }

    [Fact]
    public void SystemEvent_Create_WithPayload()
    {
        var payload = new Dictionary<string, object> { ["key"] = "value" };
        var @event = SystemEvent.Create("Test", Guid.NewGuid(), payload);

        Assert.Equal("value", @event.Payload["key"]);
    }
}
