namespace Whycespace.Runtime.EngineMetadata.Models;

using Whycespace.Contracts.Engines;

public sealed record EngineDescriptor(
    IEngine Instance,
    EngineMetadata Metadata
);
