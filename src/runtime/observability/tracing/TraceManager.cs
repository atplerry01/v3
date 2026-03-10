using System.Collections.Concurrent;

namespace Whycespace.Observability.Tracing;

public sealed class TraceManager
{
    private readonly ConcurrentDictionary<Guid, List<TraceContext>> _traces = new();

    public TraceContext StartTrace()
    {
        var traceId = Guid.NewGuid();
        var span = new TraceContext(traceId, Guid.NewGuid(), null, DateTime.UtcNow);
        _traces[traceId] = new List<TraceContext> { span };
        return span;
    }

    public TraceContext CreateSpan(Guid traceId, Guid parentSpanId)
    {
        var span = new TraceContext(traceId, Guid.NewGuid(), parentSpanId, DateTime.UtcNow);

        _traces.AddOrUpdate(
            traceId,
            _ => new List<TraceContext> { span },
            (_, spans) => { spans.Add(span); return spans; });

        return span;
    }

    public void CompleteSpan(Guid traceId, Guid spanId)
    {
        // Span completion is tracked by the presence of the span in the trace.
        // In a production system this would record duration and status.
    }

    public IReadOnlyList<TraceContext> GetSpans(Guid traceId)
    {
        return _traces.TryGetValue(traceId, out var spans)
            ? spans
            : Array.Empty<TraceContext>();
    }

    public int GetActiveTraceCount()
    {
        return _traces.Count;
    }

    public IReadOnlyCollection<Guid> GetActiveTraceIds()
    {
        return _traces.Keys.ToList();
    }
}
