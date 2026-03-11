namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceDecisionEngine
{
    private readonly VotingEngine _votingEngine;
    private readonly QuorumEngine _quorumEngine;
    private readonly GovernanceProposalStore _proposalStore;

    public GovernanceDecisionEngine(
        VotingEngine votingEngine,
        QuorumEngine quorumEngine,
        GovernanceProposalStore proposalStore)
    {
        _votingEngine = votingEngine;
        _quorumEngine = quorumEngine;
        _proposalStore = proposalStore;
    }

    public GovernanceDecision EvaluateDecision(string proposalId)
    {
        var proposal = _proposalStore.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        if (proposal.Status != ProposalStatus.Voting)
            throw new InvalidOperationException($"Proposal must be in Voting status to evaluate. Current status: {proposal.Status}");

        var tally = _votingEngine.CountVotes(proposalId);
        var quorumMet = _quorumEngine.CheckQuorum(proposalId);
        var outcome = DetermineOutcome(tally, quorumMet);

        return new GovernanceDecision(
            proposalId,
            outcome,
            tally.Approve,
            tally.Reject,
            tally.Abstain,
            quorumMet);
    }

    public DecisionOutcome DetermineOutcome(VoteTally tally, bool quorumMet)
    {
        if (!quorumMet)
            return DecisionOutcome.NoQuorum;

        return tally.Approve > tally.Reject
            ? DecisionOutcome.Approved
            : DecisionOutcome.Rejected;
    }
}
