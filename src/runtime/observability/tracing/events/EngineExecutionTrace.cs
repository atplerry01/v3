namespace Whycespace.Runtime.Observability.Tracing.Events;

public sealed record EngineExecutionTrace(
    Guid TraceId,
    string EngineName,
    string EngineTier,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    bool Success,
    int EventsProduced,
    string? FailureReason
)
{
    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : null;

    public static EngineExecutionTrace Start(string engineName, string engineTier) =>
        new(
            TraceId: Guid.NewGuid(),
            EngineName: engineName,
            EngineTier: engineTier,
            StartedAt: DateTimeOffset.UtcNow,
            CompletedAt: null,
            Success: false,
            EventsProduced: 0,
            FailureReason: null
        );

    public EngineExecutionTrace Complete(int eventsProduced) =>
        this with { CompletedAt = DateTimeOffset.UtcNow, Success = true, EventsProduced = eventsProduced };

    public EngineExecutionTrace Fail(string reason) =>
        this with { CompletedAt = DateTimeOffset.UtcNow, Success = false, FailureReason = reason };
}
