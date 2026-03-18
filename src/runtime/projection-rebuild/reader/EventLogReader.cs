
using System.Runtime.CompilerServices;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;

namespace Whycespace.ProjectionRebuild.Reader;

public sealed class EventLogReader
{
    private readonly List<EventEnvelope> _eventLog = new();
    private readonly object _lock = new();

    public void Append(EventEnvelope envelope)
    {
        lock (_lock)
        {
            _eventLog.Add(envelope);
        }
    }

    public void AppendRange(IEnumerable<EventEnvelope> envelopes)
    {
        lock (_lock)
        {
            _eventLog.AddRange(envelopes);
        }
    }

    public async IAsyncEnumerable<EventEnvelope> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EventEnvelope> snapshot;
        lock (_lock)
        {
            snapshot = _eventLog.ToList();
        }

        foreach (var envelope in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return envelope;
            await Task.CompletedTask;
        }
    }

    public async IAsyncEnumerable<EventEnvelope> ReadFromAsync(
        Guid eventId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EventEnvelope> snapshot;
        lock (_lock)
        {
            snapshot = _eventLog.ToList();
        }

        var found = false;
        foreach (var envelope in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!found)
            {
                if (envelope.EventId == eventId)
                    found = true;
                else
                    continue;
            }

            yield return envelope;
            await Task.CompletedTask;
        }
    }

    public int EventCount
    {
        get
        {
            lock (_lock)
            {
                return _eventLog.Count;
            }
        }
    }
}
