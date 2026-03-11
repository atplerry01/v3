namespace Whycespace.Engines.T0U.Governance;

using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class GovernanceEvidenceRecorder
{
    private readonly ChainEvidenceGateway _gateway;
    private const string Domain = "governance";

    public GovernanceEvidenceRecorder(ChainEvidenceGateway gateway)
    {
        _gateway = gateway;
    }

    public ChainLedgerEntry RecordProposal(GovernanceProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        return _gateway.SubmitEvidence(
            $"gov-proposal-{proposal.ProposalId}",
            Domain,
            "ProposalCreated",
            proposal);
    }

    public ChainLedgerEntry RecordVote(GovernanceVote vote)
    {
        ArgumentNullException.ThrowIfNull(vote);
        return _gateway.SubmitEvidence(
            $"gov-vote-{vote.VoteId}",
            Domain,
            "VoteCast",
            vote);
    }

    public ChainLedgerEntry RecordDecision(GovernanceDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return _gateway.SubmitEvidence(
            $"gov-decision-{decision.ProposalId}",
            Domain,
            "DecisionMade",
            decision);
    }
}
