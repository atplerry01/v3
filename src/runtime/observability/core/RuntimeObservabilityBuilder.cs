using Whycespace.Runtime.Observability.Metrics.Runtime;
using Whycespace.Runtime.Observability.Tracing.Context;

namespace Whycespace.Runtime.Observability.Core;

public sealed class RuntimeObservabilityBuilder
{
    private RuntimeMetrics? _metrics;
    private RuntimeTraceContext? _traceContext;

    public RuntimeObservabilityBuilder WithMetrics(RuntimeMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        _metrics = metrics;
        return this;
    }

    public RuntimeObservabilityBuilder WithTraceContext(RuntimeTraceContext traceContext)
    {
        ArgumentNullException.ThrowIfNull(traceContext);
        _traceContext = traceContext;
        return this;
    }

    public RuntimeObservability Build()
    {
        var metrics = _metrics ?? new RuntimeMetrics();
        var traceContext = _traceContext ?? new RuntimeTraceContext();
        var collector = new RuntimeMetricsCollector(metrics, traceContext);

        return new RuntimeObservability(metrics, traceContext, collector);
    }
}
