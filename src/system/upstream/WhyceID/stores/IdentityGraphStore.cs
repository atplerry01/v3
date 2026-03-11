namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityGraphStore
{
    private readonly ConcurrentDictionary<Guid, IdentityGraphEdge> _edges = new();

    public void Register(IdentityGraphEdge edge)
    {
        _edges[edge.EdgeId] = edge;
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetBySource(Guid identityId)
    {
        return _edges.Values
            .Where(e => e.SourceIdentityId == identityId)
            .ToList();
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetByRelationship(string relationship)
    {
        return _edges.Values
            .Where(e => e.Relationship == relationship)
            .ToList();
    }

    public void Remove(Guid edgeId)
    {
        _edges.TryRemove(edgeId, out _);
    }
}
