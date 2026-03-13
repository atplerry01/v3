namespace Whycespace.EngineManifest.Manifest;

using Whycespace.EngineManifest.Models;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EngineManifestAttribute : Attribute
{
    public string EngineName { get; }
    public EngineTier Tier { get; }
    public EngineKind Kind { get; }
    public string InputContract { get; }
    public Type[] OutputEvents { get; }

    public EngineManifestAttribute(
        string engineName,
        EngineTier tier,
        EngineKind kind,
        string inputContract,
        params Type[] outputEvents)
    {
        EngineName = engineName;
        Tier = tier;
        Kind = kind;
        InputContract = inputContract;
        OutputEvents = outputEvents;
    }
}
