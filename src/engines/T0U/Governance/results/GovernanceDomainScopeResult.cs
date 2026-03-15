namespace Whycespace.Engines.T0U.Governance.Results;

using Whycespace.System.Upstream.Governance.Models;

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
