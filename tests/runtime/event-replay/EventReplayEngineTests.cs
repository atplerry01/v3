
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EventFabric.Router;
using Whycespace.EventFabric.Topics;
using Whycespace.EventReplay.Engine;

namespace Whycespace.EventReplay.Tests;

public class EventReplayEngineTests
{
    [Fact]
    public async Task ReplayEventsAsync_Routes_All_Events()
    {
        var routed = new List<EventEnvelope>();
        var router = new EventRouter();
        router.Register(EventTopics.EngineEvents, e => { routed.Add(e); return Task.CompletedTask; });

        var engine = new EventReplayEngine(router);

        var events = new List<EventEnvelope>
        {
            CreateEnvelope("Event1", EventTopics.EngineEvents),
            CreateEnvelope("Event2", EventTopics.EngineEvents),
            CreateEnvelope("Event3", EventTopics.EngineEvents)
        };

        await engine.ReplayEventsAsync(events, CancellationToken.None);

        Assert.Equal(3, routed.Count);
        Assert.Equal(3, engine.Status.ProcessedEvents);
        Assert.False(engine.Status.Replaying);
    }

    [Fact]
    public async Task ReplayTopicAsync_Filters_By_Topic_And_TimeRange()
    {
        var routed = new List<EventEnvelope>();
        var router = new EventRouter();
        router.Register(EventTopics.EngineEvents, e => { routed.Add(e); return Task.CompletedTask; });

        var engine = new EventReplayEngine(router);

        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        var events = new List<EventEnvelope>
        {
            CreateEnvelopeAt("E1", EventTopics.EngineEvents, new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)),
            CreateEnvelopeAt("E2", EventTopics.EngineEvents, new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)),
            CreateEnvelopeAt("E3", EventTopics.WorkflowEvents, new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero))
        };

        await engine.ReplayTopicAsync(EventTopics.EngineEvents, events, from, to, CancellationToken.None);

        Assert.Single(routed);
        Assert.Equal("E1", routed[0].EventType);
    }

    [Fact]
    public async Task ReplayEventsAsync_Sets_Status_During_Replay()
    {
        var router = new EventRouter();
        var engine = new EventReplayEngine(router);

        Assert.False(engine.Status.Replaying);
        Assert.Null(engine.Status.StartedAt);

        await engine.ReplayEventsAsync(
            [CreateEnvelope("E1", EventTopics.EngineEvents)],
            CancellationToken.None);

        Assert.False(engine.Status.Replaying);
        Assert.NotNull(engine.Status.CompletedAt);
        Assert.Equal(1, engine.Status.ProcessedEvents);
    }

    private static EventEnvelope CreateEnvelope(string eventType, string topic) =>
        new(Guid.NewGuid(), eventType, topic, new { },
            new PartitionKey("pk-1"), Timestamp.Now());

    private static EventEnvelope CreateEnvelopeAt(string eventType, string topic, DateTimeOffset timestamp) =>
        new(Guid.NewGuid(), eventType, topic, new { },
            new PartitionKey("pk-1"), new Timestamp(timestamp));
}
