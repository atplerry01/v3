namespace Whycespace.Engines.T0U.Governance.Domain.Validation;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record ValidateDomainScopeCommand(
    Guid CommandId,
    Guid ProposalId,
    string AuthorityDomain,
    ProposalType ProposalType,
    Guid RequestedByGuardianId,
    DateTime Timestamp);
