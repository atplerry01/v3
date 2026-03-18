
using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;
using Whycespace.EventFabric.Topics;
using Whycespace.EventReplay.Engine;
using Whycespace.EventReplay.Models;

namespace Whycespace.EventReplay.Controller;

public sealed class ReplayController
{
    private readonly EventReplayEngine _engine;
    private readonly Func<string, DateTime, DateTime, Task<IReadOnlyList<EventEnvelope>>> _eventSource;

    public ReplayController(
        EventReplayEngine engine,
        Func<string, DateTime, DateTime, Task<IReadOnlyList<EventEnvelope>>> eventSource)
    {
        _engine = engine;
        _eventSource = eventSource;
    }

    public async Task ReplayTopicAsync(
        string topic,
        DateTime from,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTo = to ?? DateTime.UtcNow;
        var events = await _eventSource(topic, from, effectiveTo);
        await _engine.ReplayTopicAsync(topic, events, from, effectiveTo, cancellationToken);
    }

    public async Task ReplayAllAsync(
        DateTime from,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var topic in EventTopics.All)
        {
            await ReplayTopicAsync(topic, from, to, cancellationToken);
        }
    }

    public ReplayStatus GetStatus() => _engine.Status;
}
