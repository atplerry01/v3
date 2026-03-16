namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GovernanceProposalTypeStore
{
    private readonly ConcurrentDictionary<string, GovernanceProposalType> _types = new();

    public void Add(GovernanceProposalType type)
    {
        if (!_types.TryAdd(type.TypeId, type))
            throw new InvalidOperationException($"Proposal type already exists: {type.TypeId}");
    }

    public GovernanceProposalType? Get(string typeId)
    {
        _types.TryGetValue(typeId, out var type);
        return type;
    }

    public void Update(GovernanceProposalType type)
    {
        if (!_types.ContainsKey(type.TypeId))
            throw new KeyNotFoundException($"Proposal type not found: {type.TypeId}");

        _types[type.TypeId] = type;
    }

    public IReadOnlyList<GovernanceProposalType> ListAll()
    {
        return _types.Values.ToList();
    }

    public bool Exists(string typeId)
    {
        return _types.ContainsKey(typeId);
    }
}
