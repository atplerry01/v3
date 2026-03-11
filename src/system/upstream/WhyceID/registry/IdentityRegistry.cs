namespace Whycespace.System.WhyceID.Registry;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Aggregates;

public sealed class IdentityRegistry
{
    private readonly ConcurrentDictionary<Guid, IdentityAggregate> _identities = new();

    public bool Exists(Guid id)
    {
        return _identities.ContainsKey(id);
    }

    public void Register(IdentityAggregate identity)
    {
        if (!_identities.TryAdd(identity.IdentityId.Value, identity))
            throw new InvalidOperationException("Identity already exists");
    }

    public IdentityAggregate Get(Guid id)
    {
        if (!_identities.TryGetValue(id, out var identity))
            throw new KeyNotFoundException("Identity not found");

        return identity;
    }

    public int Count => _identities.Count;
}
