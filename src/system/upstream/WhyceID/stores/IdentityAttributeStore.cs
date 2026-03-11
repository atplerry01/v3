namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityAttributeStore
{
    private readonly ConcurrentDictionary<Guid, List<IdentityAttribute>> _store = new();

    public void Add(Guid identityId, IdentityAttribute attribute)
    {
        var attributes = _store.GetOrAdd(identityId, _ => new List<IdentityAttribute>());

        lock (attributes)
        {
            attributes.Add(attribute);
        }
    }

    public IReadOnlyList<IdentityAttribute> Get(Guid identityId)
    {
        if (_store.TryGetValue(identityId, out var attributes))
        {
            lock (attributes)
            {
                return attributes.ToList().AsReadOnly();
            }
        }

        return Array.Empty<IdentityAttribute>();
    }
}
