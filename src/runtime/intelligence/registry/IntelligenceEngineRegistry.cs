namespace Whycespace.IntelligenceRuntime.Registry;

using Whycespace.Contracts.Engines;
using Whycespace.IntelligenceRuntime.Models;

public sealed class IntelligenceEngineRegistry
{
    private readonly Dictionary<string, IntelligenceEngineDescriptor> _engines = new(StringComparer.Ordinal);

    public void Register(string engineId, IEngine engine, IntelligenceCapability capability)
    {
        _engines[engineId] = new IntelligenceEngineDescriptor(engineId, engine, capability);
    }

    public IntelligenceEngineDescriptor Resolve(string engineId)
    {
        if (!_engines.TryGetValue(engineId, out var descriptor))
            throw new InvalidOperationException($"Intelligence engine '{engineId}' not registered.");

        return descriptor;
    }

    public bool TryResolve(string engineId, out IntelligenceEngineDescriptor? descriptor)
        => _engines.TryGetValue(engineId, out descriptor);

    public IReadOnlyList<IntelligenceEngineDescriptor> GetByCapability(IntelligenceCapability capability)
        => _engines.Values.Where(e => e.Capability == capability).ToList();

    public IReadOnlyList<IntelligenceEngineDescriptor> GetAll()
        => _engines.Values.ToList();

    public int Count => _engines.Count;
}

public sealed record IntelligenceEngineDescriptor(
    string EngineId,
    IEngine Engine,
    IntelligenceCapability Capability
);
