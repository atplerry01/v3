namespace Whycespace.Systems.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class GovernanceAuthorityStore
{
    private readonly ConcurrentDictionary<string, List<GovernanceAuthorityRecord>> _store = new();

    public void AssignRole(GovernanceAuthorityRecord record)
    {
        _store.AddOrUpdate(
            record.ActorId,
            _ => new List<GovernanceAuthorityRecord> { record },
            (_, list) => { list.Add(record); return list; });
    }

    public IReadOnlyList<GovernanceAuthorityRecord> GetRoles(string actorId)
    {
        if (!_store.TryGetValue(actorId, out var records))
            return Array.Empty<GovernanceAuthorityRecord>();

        return records.ToList();
    }

    public bool HasRole(string actorId, GovernanceRole role)
    {
        if (!_store.TryGetValue(actorId, out var records))
            return false;

        return records.Any(r => r.Role == role);
    }
}
