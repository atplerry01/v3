namespace Whycespace.Engines.T0U.Governance.Proposal.Creation;

using Whycespace.Systems.Upstream.Governance.Models;

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
