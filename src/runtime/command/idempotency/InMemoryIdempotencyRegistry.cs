namespace Whycespace.CommandSystem.Idempotency;

using System.Collections.Concurrent;

public sealed class InMemoryIdempotencyRegistry : IIdempotencyRegistry
{
    private readonly ConcurrentDictionary<Guid, bool> _registry = new();

    public bool Exists(Guid commandId)
    {
        return _registry.ContainsKey(commandId);
    }

    public void Register(Guid commandId)
    {
        _registry[commandId] = true;
    }
}
