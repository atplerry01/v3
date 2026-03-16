namespace Whycespace.Systems.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyContextStore
{
    private readonly ConcurrentDictionary<Guid, PolicyContext> _store = new();

    public void Cache(PolicyContext context)
    {
        _store[context.ContextId] = context;
    }

    public PolicyContext? Get(Guid contextId)
    {
        _store.TryGetValue(contextId, out var context);
        return context;
    }

    public IReadOnlyList<PolicyContext> GetByActor(Guid actorId)
    {
        return _store.Values
            .Where(c => c.ActorId == actorId)
            .ToList();
    }
}
