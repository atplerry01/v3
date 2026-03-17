namespace Whycespace.IntelligenceRuntime.Models;

public sealed record IntelligenceTrace(
    Guid TraceId,
    Guid RequestId,
    IntelligenceCapability Capability,
    string EngineId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    bool? Success,
    string? Error
)
{
    public static IntelligenceTrace Start(Guid requestId, IntelligenceCapability capability, string engineId)
        => new(Guid.NewGuid(), requestId, capability, engineId, DateTimeOffset.UtcNow, null, null, null);

    public IntelligenceTrace Complete()
        => this with { CompletedAt = DateTimeOffset.UtcNow, Success = true };

    public IntelligenceTrace Fail(string error)
        => this with { CompletedAt = DateTimeOffset.UtcNow, Success = false, Error = error };

    public TimeSpan Duration => (CompletedAt ?? DateTimeOffset.UtcNow) - StartedAt;
}
