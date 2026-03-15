namespace Whycespace.Runtime.EngineManifest.Models;

public sealed class EngineRuntimeManifest
{
    public string EngineName { get; }

    public string EngineType { get; }

    public string InputContract { get; }

    public string OutputContract { get; }

    public IReadOnlyList<EngineCapability> Capabilities { get; }

    public string Version { get; }

    public EngineRuntimeManifest(
        string engineName,
        string engineType,
        string inputContract,
        string outputContract,
        IReadOnlyList<EngineCapability> capabilities,
        string version)
    {
        EngineName = engineName;
        EngineType = engineType;
        InputContract = inputContract;
        OutputContract = outputContract;
        Capabilities = capabilities;
        Version = version;
    }
}
