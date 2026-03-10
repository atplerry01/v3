namespace Whycespace.Shared.Contracts;

public sealed record EngineResult(
    bool Success,
    IReadOnlyList<EngineEvent> Events,
    IReadOnlyDictionary<string, object> Output
)
{
    public static EngineResult Ok(IReadOnlyList<EngineEvent> events, IReadOnlyDictionary<string, object>? output = null)
        => new(true, events, output ?? new Dictionary<string, object>());

    public static EngineResult Fail(string reason)
        => new(false, Array.Empty<EngineEvent>(), new Dictionary<string, object> { ["error"] = reason });
}
