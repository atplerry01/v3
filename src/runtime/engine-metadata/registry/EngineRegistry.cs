namespace Whycespace.Runtime.EngineMetadata.Registry;

using Whycespace.Runtime.EngineMetadata.Models;

public sealed class EngineRegistry
{
    private readonly Dictionary<string, EngineRegistryDescriptor> _byId;
    private readonly Dictionary<Type, EngineRegistryDescriptor> _byCommand;
    private readonly IReadOnlyList<EngineRegistryDescriptor> _all;

    internal EngineRegistry(IReadOnlyList<EngineRegistryDescriptor> descriptors)
    {
        _all = descriptors;
        _byId = descriptors.ToDictionary(d => d.EngineId);
        _byCommand = descriptors.ToDictionary(d => d.CommandType);
    }

    public EngineRegistryDescriptor GetById(string engineId)
    {
        if (_byId.TryGetValue(engineId, out var descriptor))
            return descriptor;

        throw new EngineRegistryException($"No engine registered with ID '{engineId}'.");
    }

    public EngineRegistryDescriptor GetByCommand(Type commandType)
    {
        if (_byCommand.TryGetValue(commandType, out var descriptor))
            return descriptor;

        throw new EngineRegistryException(
            $"No engine registered for command type '{commandType.FullName}'.");
    }

    public bool TryGetByCommand(Type commandType, out EngineRegistryDescriptor? descriptor)
    {
        return _byCommand.TryGetValue(commandType, out descriptor);
    }

    public IReadOnlyCollection<EngineRegistryDescriptor> GetAll() => _all;

    public EngineRegistrySnapshot Snapshot() => new(_all);

    public int Count => _all.Count;

    public bool Contains(string engineId) => _byId.ContainsKey(engineId);
}
