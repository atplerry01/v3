using Whycespace.Engines.T3I.Shared;

namespace Whycespace.Engines.T3I.Projections.Pipeline;

/// <summary>
/// Captures the outcome of a single engine execution within the ingestion pipeline.
/// </summary>
public sealed record EngineExecutionResult(
    string EngineName,
    bool Success,
    object? Output,
    string? Error,
    IntelligenceTrace Trace);