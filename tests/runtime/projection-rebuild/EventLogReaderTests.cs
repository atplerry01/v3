
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.ProjectionRebuild.Reader;

namespace Whycespace.ProjectionRebuild.Tests;

public class EventLogReaderTests
{
    private static EventEnvelope CreateEnvelope(Guid? eventId = null, string eventType = "TestEvent")
    {
        return new EventEnvelope(
            eventId ?? Guid.NewGuid(),
            eventType,
            "test-topic",
            new { },
            new PartitionKey("test"),
            new Timestamp(DateTime.UtcNow));
    }

    [Fact]
    public async Task ReadAllAsync_ReturnsAllAppendedEvents()
    {
        var reader = new EventLogReader();
        var e1 = CreateEnvelope();
        var e2 = CreateEnvelope();
        reader.Append(e1);
        reader.Append(e2);

        var results = new List<EventEnvelope>();
        await foreach (var envelope in reader.ReadAllAsync())
            results.Add(envelope);

        Assert.Equal(2, results.Count);
        Assert.Equal(e1.EventId, results[0].EventId);
        Assert.Equal(e2.EventId, results[1].EventId);
    }

    [Fact]
    public async Task ReadFromAsync_ReturnsEventsStartingFromGivenId()
    {
        var reader = new EventLogReader();
        var e1 = CreateEnvelope();
        var e2 = CreateEnvelope();
        var e3 = CreateEnvelope();
        reader.AppendRange(new[] { e1, e2, e3 });

        var results = new List<EventEnvelope>();
        await foreach (var envelope in reader.ReadFromAsync(e2.EventId))
            results.Add(envelope);

        Assert.Equal(2, results.Count);
        Assert.Equal(e2.EventId, results[0].EventId);
        Assert.Equal(e3.EventId, results[1].EventId);
    }

    [Fact]
    public void EventCount_ReflectsAppendedEvents()
    {
        var reader = new EventLogReader();
        Assert.Equal(0, reader.EventCount);

        reader.Append(CreateEnvelope());
        reader.Append(CreateEnvelope());
        Assert.Equal(2, reader.EventCount);
    }
}
