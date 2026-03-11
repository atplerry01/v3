namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceDisputeEngine
{
    private readonly GovernanceDisputeStore _disputeStore;
    private readonly GovernanceProposalStore _proposalStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceDisputeEngine(
        GovernanceDisputeStore disputeStore,
        GovernanceProposalStore proposalStore,
        GuardianRegistryStore guardianStore)
    {
        _disputeStore = disputeStore;
        _proposalStore = proposalStore;
        _guardianStore = guardianStore;
    }

    public GovernanceDispute OpenDispute(string disputeId, string proposalId, string filedBy, string reason)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        if (!_guardianStore.Exists(filedBy))
            throw new KeyNotFoundException($"Guardian not found: {filedBy}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Dispute reason is required.");

        var dispute = new GovernanceDispute(
            disputeId,
            proposalId,
            filedBy,
            reason,
            DisputeStatus.Open,
            EscalationLevel: 0,
            DateTime.UtcNow,
            ResolvedAt: null);

        _disputeStore.Add(dispute);
        return dispute;
    }

    public GovernanceDispute ResolveDispute(string disputeId)
    {
        var dispute = _disputeStore.Get(disputeId)
            ?? throw new KeyNotFoundException($"Dispute not found: {disputeId}");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Dispute is already resolved.");

        var updated = dispute with
        {
            Status = DisputeStatus.Resolved,
            ResolvedAt = DateTime.UtcNow
        };
        _disputeStore.Update(updated);
        return updated;
    }

    public GovernanceDispute EscalateDispute(string disputeId)
    {
        var dispute = _disputeStore.Get(disputeId)
            ?? throw new KeyNotFoundException($"Dispute not found: {disputeId}");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Cannot escalate a resolved dispute.");

        var updated = dispute with
        {
            Status = DisputeStatus.Escalated,
            EscalationLevel = dispute.EscalationLevel + 1
        };
        _disputeStore.Update(updated);
        return updated;
    }
}
