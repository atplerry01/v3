namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceVote(
    string VoteId,
    string ProposalId,
    string GuardianId,
    VoteType Vote,
    int VoteWeight,
    DateTime Timestamp);

public enum VoteType
{
    Approve,
    Reject,
    Abstain
}

public enum VoteAction
{
    Cast,
    Withdrawn,
    Validated
}
