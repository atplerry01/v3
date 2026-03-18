namespace Whycespace.IntelligenceRuntime.Orchestrator;

using System.Diagnostics;
using Whycespace.IntelligenceRuntime.Models;

/// <summary>
/// Executes all intelligence capabilities in a deterministic sequence:
/// Atlas → Forecasting → Monitoring → Reporting.
/// No engine-to-engine calls — the pipeline controls execution order
/// and feeds shared context to each capability phase.
/// </summary>
public sealed class IntelligencePipeline
{
    private static readonly IntelligenceCapability[] ExecutionOrder =
    [
        IntelligenceCapability.Atlas,
        IntelligenceCapability.Forecasting,
        IntelligenceCapability.Monitoring,
        IntelligenceCapability.Reporting
    ];

    private readonly IntelligenceOrchestrator _orchestrator;
    private readonly IntelligenceContextBuilder _contextBuilder;

    public IntelligencePipeline(
        IntelligenceOrchestrator orchestrator,
        IntelligenceContextBuilder contextBuilder)
    {
        _orchestrator = orchestrator;
        _contextBuilder = contextBuilder;
    }

    public async Task<IntelligencePipelineResult> ExecuteAsync(
        IReadOnlyDictionary<string, object> projectionData,
        string? correlationId = null,
        string? partitionKey = null)
    {
        var pipelineContext = _contextBuilder.Build(projectionData, correlationId, partitionKey);
        var sw = Stopwatch.StartNew();
        var resultsByCapability = new Dictionary<IntelligenceCapability, IReadOnlyList<IntelligenceResult>>();

        try
        {
            foreach (var capability in ExecutionOrder)
            {
                var capabilityContext = _contextBuilder.BuildForCapability(
                    capability, pipelineContext);

                var results = await _orchestrator.ExecuteCapabilityAsync(
                    capability, capabilityContext.Parameters);

                resultsByCapability[capability] = results;

                PropagateOutputs(pipelineContext, results);
            }

            sw.Stop();

            var anyFailure = resultsByCapability.Values
                .SelectMany(r => r)
                .Any(r => !r.Success);

            return anyFailure
                ? IntelligencePipelineResult.Fail(
                    pipelineContext.PipelineId,
                    pipelineContext.CorrelationId,
                    "One or more engines failed during pipeline execution.",
                    resultsByCapability,
                    sw.Elapsed)
                : IntelligencePipelineResult.Ok(
                    pipelineContext.PipelineId,
                    pipelineContext.CorrelationId,
                    resultsByCapability,
                    sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return IntelligencePipelineResult.Fail(
                pipelineContext.PipelineId,
                pipelineContext.CorrelationId,
                ex.Message,
                resultsByCapability,
                sw.Elapsed);
        }
    }

    /// <summary>
    /// Feeds successful engine outputs back into the shared pipeline context
    /// so downstream capabilities can consume upstream results.
    /// </summary>
    private static void PropagateOutputs(
        IntelligencePipelineContext pipelineContext,
        IReadOnlyList<IntelligenceResult> results)
    {
        if (pipelineContext.Parameters is not Dictionary<string, object> mutableParams)
            return;

        foreach (var result in results)
        {
            if (!result.Success) continue;

            var outputKey = $"{result.Capability}.{result.EngineId}";
            mutableParams[outputKey] = result.Output;
        }
    }
}
