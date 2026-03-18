namespace Whycespace.Engines.T3I.Shared;

/// <summary>
/// Captures execution trace data for observability and audit.
/// </summary>
public sealed record IntelligenceTrace(
    string EngineName,
    Guid CorrelationId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyDictionary<string, object>? Metadata = null)
{
    public static IntelligenceTrace Create(string engineName, Guid correlationId, DateTimeOffset startedAt)
        => new(engineName, correlationId, startedAt, DateTimeOffset.UtcNow);

    public static IntelligenceTrace Create(
        string engineName,
        Guid correlationId,
        DateTimeOffset startedAt,
        IReadOnlyDictionary<string, object> metadata)
        => new(engineName, correlationId, startedAt, DateTimeOffset.UtcNow, metadata);
}
