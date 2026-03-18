namespace Whycespace.IntelligenceRuntime.Orchestrator;

using Whycespace.IntelligenceRuntime.Models;

/// <summary>
/// Builds execution context for the intelligence pipeline from projection data.
/// Merges projection snapshots with correlation metadata into a parameter set
/// that the orchestrator distributes to each capability's engines.
/// </summary>
public sealed class IntelligenceContextBuilder
{
    public IntelligencePipelineContext Build(
        IReadOnlyDictionary<string, object> projectionData,
        string? correlationId = null,
        string? partitionKey = null)
    {
        var pipelineId = Guid.NewGuid();
        var resolvedCorrelationId = correlationId ?? pipelineId.ToString();

        var parameters = new Dictionary<string, object>(projectionData)
        {
            ["PipelineId"] = pipelineId,
            ["CorrelationId"] = resolvedCorrelationId,
            ["PipelineTimestamp"] = DateTimeOffset.UtcNow
        };

        return new IntelligencePipelineContext(
            pipelineId,
            resolvedCorrelationId,
            partitionKey,
            parameters);
    }

    public IntelligencePipelineContext BuildForCapability(
        IntelligenceCapability capability,
        IntelligencePipelineContext pipelineContext,
        IReadOnlyDictionary<string, object>? capabilityOverrides = null)
    {
        var parameters = new Dictionary<string, object>(pipelineContext.Parameters)
        {
            ["CurrentCapability"] = capability.ToString()
        };

        if (capabilityOverrides is not null)
        {
            foreach (var kvp in capabilityOverrides)
                parameters[kvp.Key] = kvp.Value;
        }

        return pipelineContext with { Parameters = parameters };
    }
}

public sealed record IntelligencePipelineContext(
    Guid PipelineId,
    string CorrelationId,
    string? PartitionKey,
    IReadOnlyDictionary<string, object> Parameters
);
