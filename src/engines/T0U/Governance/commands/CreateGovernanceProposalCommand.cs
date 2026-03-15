namespace Whycespace.Engines.T0U.Governance.Commands;

using Whycespace.System.Upstream.Governance.Models;

public sealed record CreateGovernanceProposalCommand(
    Guid CommandId,
    Guid ProposalId,
    string ProposalTitle,
    string ProposalDescription,
    ProposalType ProposalType,
    string AuthorityDomain,
    Guid ProposedByGuardianId,
    Dictionary<string, string> Metadata,
    DateTime Timestamp);
