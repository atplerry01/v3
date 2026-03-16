namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityGraphEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityGraphStore _store;

    public IdentityGraphEngine(
        IdentityRegistry registry,
        IdentityGraphStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityGraphEdge CreateRelationship(
        Guid sourceIdentityId,
        Guid targetEntityId,
        string relationship)
    {
        if (!_registry.Exists(sourceIdentityId))
            throw new InvalidOperationException($"Identity does not exist: {sourceIdentityId}");

        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Relationship cannot be empty.");

        var edge = new IdentityGraphEdge(
            Guid.NewGuid(),
            sourceIdentityId,
            targetEntityId,
            relationship,
            DateTime.UtcNow
        );

        _store.Register(edge);

        return edge;
    }

    public void RemoveRelationship(Guid edgeId)
    {
        _store.Remove(edgeId);
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetRelationships(Guid identityId)
    {
        return _store.GetBySource(identityId);
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetRelationshipsByType(string relationship)
    {
        return _store.GetByRelationship(relationship);
    }
}
