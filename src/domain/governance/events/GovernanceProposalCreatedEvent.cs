namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceProposalCreatedEvent(
    Guid EventId,
    Guid ProposalId,
    string ProposalTitle,
    string ProposalDescription,
    string ProposalType,
    string AuthorityDomain,
    Guid ProposedByGuardianId,
    DateTimeOffset CreatedAt,
    Dictionary<string, string> Metadata);
