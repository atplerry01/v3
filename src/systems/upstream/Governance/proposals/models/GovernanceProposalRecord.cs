namespace Whycespace.Systems.Upstream.Governance.Proposals.Models;

public sealed record GovernanceProposalRecord(
    Guid ProposalId,
    string ProposalTitle,
    string ProposalDescription,
    GovernanceProposalType ProposalType,
    GovernanceProposalStatus ProposalStatus,
    string AuthorityDomain,
    Guid ProposedByGuardianId,
    DateTime CreatedAt,
    DateTime? VotingStart,
    DateTime? VotingEnd,
    DateTime? DecisionTimestamp,
    Dictionary<string, string> Metadata);
