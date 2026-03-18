
namespace Whycespace.IntelligenceRuntime;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.IntelligenceRuntime.Models;
using Whycespace.IntelligenceRuntime.Orchestrator;
using Whycespace.IntelligenceRuntime.Projections;
using Whycespace.IntelligenceRuntime.Registry;
using Whycespace.IntelligenceRuntime.Routing;
using Whycespace.IntelligenceRuntime.Tracing;

public sealed class IntelligenceRuntimeBootstrapper
{
    public IntelligenceEngineRegistry Registry { get; }
    public IntelligenceRouter Router { get; }
    public IntelligenceOrchestrator Orchestrator { get; }
    public IntelligenceContextBuilder ContextBuilder { get; }
    public IntelligencePipeline Pipeline { get; }
    public IntelligenceProjectionRouter ProjectionRouter { get; }
    public IntelligenceProjection Projection { get; }
    public IntelligenceInsightStore InsightStore { get; }
    public IntelligenceTraceCollector TraceCollector { get; }
    public IntelligenceObservability Observability { get; }

    public IntelligenceRuntimeBootstrapper()
    {
        Registry = new IntelligenceEngineRegistry();
        Router = new IntelligenceRouter(Registry);
        TraceCollector = new IntelligenceTraceCollector();
        Orchestrator = new IntelligenceOrchestrator(Router, TraceCollector);
        ContextBuilder = new IntelligenceContextBuilder();
        Pipeline = new IntelligencePipeline(Orchestrator, ContextBuilder);
        ProjectionRouter = new IntelligenceProjectionRouter(Orchestrator);
        Projection = new IntelligenceProjection(ProjectionRouter);
        InsightStore = new IntelligenceInsightStore();
        Observability = new IntelligenceObservability(TraceCollector);
    }

    public IntelligenceRuntimeBootstrapper RegisterEngine(
        string engineId, IEngine engine, IntelligenceCapability capability)
    {
        Registry.Register(engineId, engine, capability);
        return this;
    }

    public IntelligenceRuntimeBootstrapper BindProjection(
        string eventType,
        IntelligenceCapability capability,
        string engineId,
        Func<EventEnvelope, IReadOnlyDictionary<string, object>> parameterExtractor)
    {
        ProjectionRouter.Bind(eventType, capability, engineId, parameterExtractor);
        return this;
    }
}
