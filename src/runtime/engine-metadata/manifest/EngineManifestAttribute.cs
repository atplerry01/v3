namespace Whycespace.Runtime.EngineMetadata.Manifest;

using Whycespace.Runtime.EngineMetadata.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EngineManifestAttribute : Attribute
{
    public string EngineId { get; }
    public EngineTier Tier { get; }
    public EngineKind Kind { get; }
    public string InputContract { get; }
    public Type[] OutputEvents { get; }
    public Type CommandType { get; }
    public Type ResultType { get; }

    public EngineManifestAttribute(
        string engineId,
        EngineTier tier,
        Type commandType,
        Type resultType,
        EngineKind kind = EngineKind.Decision,
        string inputContract = "",
        params Type[] outputEvents)
    {
        EngineId = engineId;
        Tier = tier;
        Kind = kind;
        InputContract = inputContract;
        OutputEvents = outputEvents;
        CommandType = commandType;
        ResultType = resultType;
    }
}
