namespace Whycespace.Engines.T0U.WhyceGovernance.Results;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceProposalResult(
    bool Success,
    Guid ProposalId,
    ProposalType ProposalType,
    string AuthorityDomain,
    GovernanceProposalAction Action,
    string Message,
    DateTime ExecutedAt);

public enum GovernanceProposalAction
{
    Created,
    Submitted,
    Cancelled
}
