using Whycespace.Runtime.Observability.Metrics.Runtime;
using Whycespace.Runtime.Observability.Tracing.Context;

namespace Whycespace.Runtime.Observability.Core;

public sealed class RuntimeObservability
{
    public RuntimeMetrics Metrics { get; }
    public RuntimeTraceContext TraceContext { get; }
    public RuntimeMetricsCollector Collector { get; }

    internal RuntimeObservability(
        RuntimeMetrics metrics,
        RuntimeTraceContext traceContext,
        RuntimeMetricsCollector collector)
    {
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        TraceContext = traceContext ?? throw new ArgumentNullException(nameof(traceContext));
        Collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }

    public RuntimeMetricsSnapshot Snapshot() => RuntimeMetricsSnapshot.From(Metrics);
}
