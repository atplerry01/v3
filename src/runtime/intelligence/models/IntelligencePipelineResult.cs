namespace Whycespace.IntelligenceRuntime.Models;

/// <summary>
/// Aggregated result from executing all intelligence capabilities in a single pipeline run.
/// </summary>
public sealed record IntelligencePipelineResult(
    Guid PipelineId,
    string CorrelationId,
    bool Success,
    IReadOnlyDictionary<IntelligenceCapability, IReadOnlyList<IntelligenceResult>> ResultsByCapability,
    TimeSpan TotalDuration,
    string? Error = null
)
{
    public int TotalEnginesExecuted => ResultsByCapability.Values.Sum(r => r.Count);

    public int SucceededCount => ResultsByCapability.Values
        .SelectMany(r => r)
        .Count(r => r.Success);

    public int FailedCount => ResultsByCapability.Values
        .SelectMany(r => r)
        .Count(r => !r.Success);

    public IReadOnlyList<IntelligenceResult> GetCapabilityResults(IntelligenceCapability capability)
        => ResultsByCapability.TryGetValue(capability, out var results) ? results : [];

    public static IntelligencePipelineResult Ok(
        Guid pipelineId,
        string correlationId,
        IReadOnlyDictionary<IntelligenceCapability, IReadOnlyList<IntelligenceResult>> results,
        TimeSpan duration)
        => new(pipelineId, correlationId, true, results, duration);

    public static IntelligencePipelineResult Fail(
        Guid pipelineId,
        string correlationId,
        string error,
        IReadOnlyDictionary<IntelligenceCapability, IReadOnlyList<IntelligenceResult>> partialResults,
        TimeSpan duration)
        => new(pipelineId, correlationId, false, partialResults, duration, error);
}
