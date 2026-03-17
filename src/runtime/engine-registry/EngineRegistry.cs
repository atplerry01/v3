namespace Whycespace.Runtime.EngineRegistry;

public sealed class EngineRegistry
{
    private readonly Dictionary<string, EngineDescriptor> _byId;
    private readonly Dictionary<Type, EngineDescriptor> _byCommand;
    private readonly IReadOnlyList<EngineDescriptor> _all;

    internal EngineRegistry(IReadOnlyList<EngineDescriptor> descriptors)
    {
        _all = descriptors;
        _byId = descriptors.ToDictionary(d => d.EngineId);
        _byCommand = descriptors.ToDictionary(d => d.CommandType);
    }

    public EngineDescriptor GetById(string engineId)
    {
        if (_byId.TryGetValue(engineId, out var descriptor))
            return descriptor;

        throw new EngineRegistryException($"No engine registered with ID '{engineId}'.");
    }

    public EngineDescriptor GetByCommand(Type commandType)
    {
        if (_byCommand.TryGetValue(commandType, out var descriptor))
            return descriptor;

        throw new EngineRegistryException(
            $"No engine registered for command type '{commandType.FullName}'.");
    }

    public bool TryGetByCommand(Type commandType, out EngineDescriptor? descriptor)
    {
        return _byCommand.TryGetValue(commandType, out descriptor);
    }

    public IReadOnlyCollection<EngineDescriptor> GetAll() => _all;

    public EngineRegistrySnapshot Snapshot() => new(_all);

    public int Count => _all.Count;

    public bool Contains(string engineId) => _byId.ContainsKey(engineId);
}
