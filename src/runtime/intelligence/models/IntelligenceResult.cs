namespace Whycespace.IntelligenceRuntime.Models;

public sealed record IntelligenceResult(
    Guid RequestId,
    bool Success,
    IntelligenceCapability Capability,
    string EngineId,
    IReadOnlyDictionary<string, object> Output,
    TimeSpan Duration,
    string? Error = null
)
{
    public static IntelligenceResult Ok(
        Guid requestId,
        IntelligenceCapability capability,
        string engineId,
        IReadOnlyDictionary<string, object> output,
        TimeSpan duration)
        => new(requestId, true, capability, engineId, output, duration);

    public static IntelligenceResult Fail(
        Guid requestId,
        IntelligenceCapability capability,
        string engineId,
        string error,
        TimeSpan duration)
        => new(requestId, false, capability, engineId,
            new Dictionary<string, object>(), duration, error);
}
