namespace Whycespace.Runtime.EngineMetadata.Models;

public sealed record EngineRegistryDescriptor(
    string EngineId,
    EngineTier Tier,
    Type EngineType,
    Type CommandType,
    Type ResultType
);
