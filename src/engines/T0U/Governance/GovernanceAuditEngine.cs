namespace Whycespace.Engines.T0U.Governance;

using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceAuditEngine
{
    private readonly GovernanceProposalStore _proposalStore;
    private readonly GovernanceVoteStore _voteStore;
    private readonly ChainEvidenceGateway _gateway;

    public GovernanceAuditEngine(
        GovernanceProposalStore proposalStore,
        GovernanceVoteStore voteStore,
        ChainEvidenceGateway gateway)
    {
        _proposalStore = proposalStore;
        _voteStore = voteStore;
        _gateway = gateway;
    }

    public GovernanceAuditResult AuditProposal(string proposalId)
    {
        var proposal = _proposalStore.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var findings = new List<string>();
        var hasEvidence = false;

        try
        {
            var evidence = _gateway.GetEvidence($"gov-proposal-{proposalId}");
            hasEvidence = true;
            var verified = _gateway.VerifyEvidence($"gov-proposal-{proposalId}", proposal);
            if (!verified)
                findings.Add("Proposal evidence hash mismatch — record may have been tampered with.");
        }
        catch
        {
            findings.Add("No evidence recorded for proposal.");
        }

        return new GovernanceAuditResult(
            $"audit-proposal-{proposalId}",
            proposalId,
            AuditTargetType.Proposal,
            hasEvidence,
            IsValid: hasEvidence && findings.Count == 0,
            findings,
            DateTime.UtcNow);
    }

    public IReadOnlyList<GovernanceAuditResult> AuditVotes(string proposalId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var votes = _voteStore.GetByProposal(proposalId);
        var results = new List<GovernanceAuditResult>();

        foreach (var vote in votes)
        {
            var findings = new List<string>();
            var hasEvidence = false;

            try
            {
                _gateway.GetEvidence($"gov-vote-{vote.VoteId}");
                hasEvidence = true;
                var verified = _gateway.VerifyEvidence($"gov-vote-{vote.VoteId}", vote);
                if (!verified)
                    findings.Add($"Vote evidence hash mismatch for vote '{vote.VoteId}'.");
            }
            catch
            {
                findings.Add($"No evidence recorded for vote '{vote.VoteId}'.");
            }

            results.Add(new GovernanceAuditResult(
                $"audit-vote-{vote.VoteId}",
                vote.VoteId,
                AuditTargetType.Vote,
                hasEvidence,
                IsValid: hasEvidence && findings.Count == 0,
                findings,
                DateTime.UtcNow));
        }

        return results;
    }

    public GovernanceAuditResult AuditDecision(string proposalId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var findings = new List<string>();
        var hasEvidence = false;

        try
        {
            _gateway.GetEvidence($"gov-decision-{proposalId}");
            hasEvidence = true;
        }
        catch
        {
            findings.Add("No evidence recorded for decision.");
        }

        return new GovernanceAuditResult(
            $"audit-decision-{proposalId}",
            proposalId,
            AuditTargetType.Decision,
            hasEvidence,
            IsValid: hasEvidence && findings.Count == 0,
            findings,
            DateTime.UtcNow);
    }
}
