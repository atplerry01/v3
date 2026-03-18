namespace Whycespace.Engines.T3I.Projections.Pipeline;

/// <summary>
/// Result of the full event ingestion pipeline: projection application + engine orchestration.
/// </summary>
public sealed record IngestionResult(
    Guid CorrelationId,
    Guid EventId,
    string EventType,
    bool ProjectionApplied,
    IReadOnlyList<EngineExecutionResult> EngineResults,
    DateTimeOffset Timestamp)
{
    public bool AllEnginesSucceeded => EngineResults.All(r => r.Success);

    public static IngestionResult Empty(Guid correlationId, Guid eventId, string eventType, bool projectionApplied)
        => new(correlationId, eventId, eventType, projectionApplied, [], DateTimeOffset.UtcNow);
}