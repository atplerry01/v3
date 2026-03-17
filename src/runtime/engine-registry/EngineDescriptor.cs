namespace Whycespace.Runtime.EngineRegistry;

public sealed record EngineDescriptor(
    string EngineId,
    EngineTier Tier,
    Type EngineType,
    Type CommandType,
    Type ResultType
);
