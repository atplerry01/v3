namespace Whycespace.Runtime.EngineManifest.Models;

public sealed record EngineMetadata(
    string EngineName,
    EngineTier Tier,
    EngineKind Kind,
    string InputContract,
    IReadOnlyCollection<string> OutputEvents
);
