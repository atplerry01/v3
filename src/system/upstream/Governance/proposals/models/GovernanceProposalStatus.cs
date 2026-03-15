namespace Whycespace.System.Upstream.Governance.Proposals.Models;

public enum GovernanceProposalStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    VotingOpen = 3,
    VotingClosed = 4,
    Approved = 5,
    Rejected = 6,
    Cancelled = 7
}
