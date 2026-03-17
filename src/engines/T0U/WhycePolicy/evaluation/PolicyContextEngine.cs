namespace Whycespace.Engines.T0U.WhycePolicy.Evaluation;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyContextEngine
{
    private readonly PolicyContextStore _store;

    public PolicyContextEngine(PolicyContextStore store)
    {
        _store = store;
    }

    public PolicyContext BuildContext(Guid actorId, string targetDomain, IReadOnlyDictionary<string, string> attributes)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(targetDomain))
            throw new ArgumentException("Target domain cannot be empty.");

        if (attributes is null || attributes.Count == 0)
            throw new ArgumentException("Attributes cannot be empty.");

        var context = new PolicyContext(
            Guid.NewGuid(),
            actorId,
            targetDomain,
            attributes,
            DateTime.UtcNow
        );

        _store.Cache(context);
        return context;
    }

    public PolicyContext GetContext(Guid contextId)
    {
        var context = _store.Get(contextId);
        if (context is null)
            throw new KeyNotFoundException($"Context not found: '{contextId}'.");
        return context;
    }

    public IReadOnlyList<PolicyContext> GetContextsByActor(Guid actorId)
    {
        return _store.GetByActor(actorId);
    }
}
