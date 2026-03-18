namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceProposal(
    string ProposalId,
    string Title,
    string Description,
    ProposalType Type,
    string CreatedBy,
    DateTime CreatedAt,
    ProposalStatus Status);

public enum ProposalType
{
    Constitutional,
    Policy,
    Operational
}

public enum ProposalStatus
{
    Draft = 0,
    Open = 1,
    Voting = 2,
    Approved = 3,
    Rejected = 4,
    Closed = 5,
    Cancelled = 6
}
