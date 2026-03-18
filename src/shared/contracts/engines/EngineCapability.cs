namespace Whycespace.Contracts.Engines;

public sealed record EngineCapability(
    string Name,
    string Description,
    IReadOnlyList<string> SupportedEventTypes,
    bool IsIdempotent = true,
    bool IsStateless = true
);
