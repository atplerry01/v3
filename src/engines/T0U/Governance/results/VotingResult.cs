namespace Whycespace.Engines.T0U.Governance.Results;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record VotingResult(
    bool Success,
    string VoteId,
    string ProposalId,
    string GuardianId,
    VoteType VoteDecision,
    VoteAction Action,
    string Message,
    DateTime ExecutedAt)
{
    public static VotingResult Ok(
        string voteId,
        string proposalId,
        string guardianId,
        VoteType voteDecision,
        VoteAction action,
        string message)
        => new(true, voteId, proposalId, guardianId, voteDecision, action, message, DateTime.UtcNow);

    public static VotingResult Fail(
        string proposalId,
        string guardianId,
        VoteAction action,
        string message)
        => new(false, string.Empty, proposalId, guardianId, VoteType.Abstain, action, message, DateTime.UtcNow);
}
