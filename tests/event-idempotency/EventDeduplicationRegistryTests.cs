using Whycespace.EventIdempotency.Models;
using Whycespace.EventIdempotency.Registry;

namespace Whycespace.EventIdempotency.Tests;

public class EventDeduplicationRegistryTests
{
    [Fact]
    public void HasProcessed_Returns_False_For_New_Event()
    {
        var registry = new EventDeduplicationRegistry();
        Assert.False(registry.HasProcessed(Guid.NewGuid()));
    }

    [Fact]
    public void MarkProcessed_Then_HasProcessed_Returns_True()
    {
        var registry = new EventDeduplicationRegistry();
        var eventId = Guid.NewGuid();

        registry.MarkProcessed(new ProcessedEvent(eventId, "TestEvent", "whyce.engine.events", "pk-1"));

        Assert.True(registry.HasProcessed(eventId));
    }

    [Fact]
    public void ProcessedCount_Tracks_Entries()
    {
        var registry = new EventDeduplicationRegistry();

        registry.MarkProcessed(new ProcessedEvent(Guid.NewGuid(), "A", "t", "pk"));
        registry.MarkProcessed(new ProcessedEvent(Guid.NewGuid(), "B", "t", "pk"));

        Assert.Equal(2, registry.ProcessedCount);
    }

    [Fact]
    public void GetProcessedEvent_Returns_Entry()
    {
        var registry = new EventDeduplicationRegistry();
        var eventId = Guid.NewGuid();

        registry.MarkProcessed(new ProcessedEvent(eventId, "TestEvent", "whyce.engine.events", "pk-1"));

        var entry = registry.GetProcessedEvent(eventId);
        Assert.NotNull(entry);
        Assert.Equal("TestEvent", entry.EventType);
    }
}
