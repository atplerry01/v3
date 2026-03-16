namespace Whycespace.Systems.WhyceID.Registry;

using global::System.Collections.Concurrent;
using Whycespace.Systems.WhyceID.Aggregates;

public sealed class IdentityRegistry
{
    private readonly ConcurrentDictionary<Guid, IdentityAggregate> _identities = new();

    public void Register(IdentityAggregate identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!_identities.TryAdd(identity.Id.Value, identity))
        {
            throw new InvalidOperationException(
                $"Identity already registered: {identity.Id.Value}");
        }
    }

    public IdentityAggregate Get(Guid identityId)
    {
        if (!_identities.TryGetValue(identityId, out var identity))
        {
            throw new KeyNotFoundException(
                $"Identity not found: {identityId}");
        }

        return identity;
    }

    public bool Exists(Guid identityId)
    {
        return _identities.ContainsKey(identityId);
    }

    public void Update(IdentityAggregate identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        _identities[identity.Id.Value] = identity;
    }

    public IReadOnlyCollection<IdentityAggregate> GetAll()
    {
        return _identities.Values.ToList().AsReadOnly();
    }

    public int Count => _identities.Count;
}
