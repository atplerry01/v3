namespace Whycespace.IntelligenceRuntime.Orchestrator;

using System.Diagnostics;
using Whycespace.Contracts.Engines;
using Whycespace.IntelligenceRuntime.Models;
using Whycespace.IntelligenceRuntime.Routing;
using Whycespace.IntelligenceRuntime.Tracing;

public sealed class IntelligenceOrchestrator
{
    private readonly IntelligenceRouter _router;
    private readonly IntelligenceTraceCollector _traceCollector;

    public IntelligenceOrchestrator(
        IntelligenceRouter router,
        IntelligenceTraceCollector traceCollector)
    {
        _router = router;
        _traceCollector = traceCollector;
    }

    public async Task<IntelligenceResult> ExecuteAsync(IntelligenceRequest request)
    {
        var descriptor = _router.Route(request);
        var trace = IntelligenceTrace.Start(request.RequestId, request.Capability, request.EngineId);
        _traceCollector.Record(trace);

        var sw = Stopwatch.StartNew();

        try
        {
            var context = BuildContext(request);
            var engineResult = await descriptor.Engine.ExecuteAsync(context);
            sw.Stop();

            _traceCollector.Record(trace.Complete());

            return IntelligenceResult.Ok(
                request.RequestId,
                request.Capability,
                request.EngineId,
                engineResult.Output,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _traceCollector.Record(trace.Fail(ex.Message));

            return IntelligenceResult.Fail(
                request.RequestId,
                request.Capability,
                request.EngineId,
                ex.Message,
                sw.Elapsed);
        }
    }

    public async Task<IReadOnlyList<IntelligenceResult>> ExecuteCapabilityAsync(
        IntelligenceCapability capability,
        IReadOnlyDictionary<string, object> sharedParameters)
    {
        var engines = _router.ResolveCapability(capability);
        var results = new List<IntelligenceResult>(engines.Count);

        foreach (var engine in engines)
        {
            var request = IntelligenceRequest.Create(capability, engine.EngineId, sharedParameters);
            var result = await ExecuteAsync(request);
            results.Add(result);
        }

        return results;
    }

    private static EngineContext BuildContext(IntelligenceRequest request)
    {
        return new EngineContext(
            request.RequestId,
            request.CorrelationId ?? Guid.NewGuid().ToString(),
            $"Intelligence.{request.Capability}.{request.EngineId}",
            request.PartitionKey ?? "intelligence-0",
            new Dictionary<string, object>(request.Parameters));
    }
}
