namespace Whycespace.Engines.T0U.WhyceGovernance.Results;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceDomainScopeResult(
    bool Success,
    string AuthorityDomain,
    Guid ProposalId,
    ProposalType ProposalType,
    DomainScopeAction Action,
    string Message,
    DateTime ExecutedAt);

public enum DomainScopeAction
{
    Registered,
    Deactivated,
    Validated
}
