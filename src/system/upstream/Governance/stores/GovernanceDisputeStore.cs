namespace Whycespace.System.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.Governance.Models;

public sealed class GovernanceDisputeStore
{
    private readonly ConcurrentDictionary<string, GovernanceDispute> _disputes = new();

    public void Add(GovernanceDispute dispute)
    {
        if (!_disputes.TryAdd(dispute.DisputeId, dispute))
            throw new InvalidOperationException($"Dispute already exists: {dispute.DisputeId}");
    }

    public GovernanceDispute? Get(string disputeId)
    {
        _disputes.TryGetValue(disputeId, out var dispute);
        return dispute;
    }

    public void Update(GovernanceDispute dispute)
    {
        if (!_disputes.ContainsKey(dispute.DisputeId))
            throw new KeyNotFoundException($"Dispute not found: {dispute.DisputeId}");

        _disputes[dispute.DisputeId] = dispute;
    }
}
