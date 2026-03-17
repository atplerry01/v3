namespace Whycespace.Runtime.EventFabricGuard;

public sealed record EventPublishContext(
    object EventInstance,
    string? SourceEngineId = null,
    string? CorrelationId = null,
    DateTimeOffset? Timestamp = null
)
{
    public DateTimeOffset EffectiveTimestamp => Timestamp ?? DateTimeOffset.UtcNow;
}
