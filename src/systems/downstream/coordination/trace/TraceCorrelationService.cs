namespace Whycespace.Systems.Downstream.Coordination.Trace;

public sealed class TraceCorrelationService
{
    private readonly Dictionary<string, DownstreamTraceContext> _activeTraces = new();

    public DownstreamTraceContext StartTrace(string operationName)
    {
        var trace = new DownstreamTraceContext(operationName);
        _activeTraces[trace.TraceId] = trace;
        return trace;
    }

    public DownstreamTraceContext? GetTrace(string traceId)
    {
        _activeTraces.TryGetValue(traceId, out var trace);
        return trace;
    }

    public void CompleteTrace(string traceId, bool success)
    {
        if (_activeTraces.TryGetValue(traceId, out var trace))
        {
            trace.Complete(success);
        }
    }

    public IReadOnlyList<DownstreamTraceContext> GetActiveTraces()
        => _activeTraces.Values.Where(t => t.CompletedAt is null).ToList();

    public IReadOnlyList<DownstreamTraceContext> GetCompletedTraces()
        => _activeTraces.Values.Where(t => t.CompletedAt is not null).ToList();
}
