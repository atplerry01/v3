namespace Whycespace.Engines.T3I.Shared;

/// <summary>
/// Standardised result envelope for all T3I intelligence engine executions.
/// </summary>
public sealed record IntelligenceResult<TOutput>(
    bool Success,
    TOutput? Output,
    string? Error,
    IntelligenceTrace Trace)
{
    public static IntelligenceResult<TOutput> Ok(TOutput output, IntelligenceTrace trace)
        => new(true, output, null, trace);

    public static IntelligenceResult<TOutput> Fail(string error, IntelligenceTrace trace)
        => new(false, default, error, trace);
}
