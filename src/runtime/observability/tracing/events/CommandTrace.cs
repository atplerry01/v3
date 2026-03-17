namespace Whycespace.Runtime.Observability.Tracing.Events;

public sealed record CommandTrace(
    Guid TraceId,
    Guid CommandId,
    string CommandType,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    bool Success,
    string? FailureReason
)
{
    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : null;

    public static CommandTrace Start(Guid commandId, string commandType) =>
        new(
            TraceId: Guid.NewGuid(),
            CommandId: commandId,
            CommandType: commandType,
            StartedAt: DateTimeOffset.UtcNow,
            CompletedAt: null,
            Success: false,
            FailureReason: null
        );

    public CommandTrace Complete() =>
        this with { CompletedAt = DateTimeOffset.UtcNow, Success = true };

    public CommandTrace Fail(string reason) =>
        this with { CompletedAt = DateTimeOffset.UtcNow, Success = false, FailureReason = reason };
}