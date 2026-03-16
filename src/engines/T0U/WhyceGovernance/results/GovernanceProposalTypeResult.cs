namespace Whycespace.Engines.T0U.WhyceGovernance.Results;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceProposalTypeResult(
    bool Success,
    string ProposalType,
    GovernanceProposalTypeAction Action,
    string AuthorityDomain,
    string Message,
    DateTime ExecutedAt);
