namespace Whycespace.Engines.T3I.Shared;

/// <summary>
/// Wraps engine input with correlation and timestamp metadata.
/// </summary>
public sealed record IntelligenceContext<TInput>(
    Guid CorrelationId,
    TInput Input,
    DateTimeOffset Timestamp)
{
    public static IntelligenceContext<TInput> Create(TInput input)
        => new(Guid.NewGuid(), input, DateTimeOffset.UtcNow);

    public static IntelligenceContext<TInput> Create(Guid correlationId, TInput input)
        => new(correlationId, input, DateTimeOffset.UtcNow);
}
