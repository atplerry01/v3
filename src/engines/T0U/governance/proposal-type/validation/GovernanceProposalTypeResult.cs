namespace Whycespace.Engines.T0U.Governance.ProposalType.Validation;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceProposalTypeResult(
    bool Success,
    string ProposalType,
    GovernanceProposalTypeAction Action,
    string AuthorityDomain,
    string Message,
    DateTime ExecutedAt);
