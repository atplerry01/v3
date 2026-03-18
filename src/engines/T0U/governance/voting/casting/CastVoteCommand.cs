namespace Whycespace.Engines.T0U.Governance.Voting.Casting;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record CastVoteCommand(
    string CommandId,
    string ProposalId,
    string GuardianId,
    VoteType VoteDecision,
    int VoteWeight,
    DateTime Timestamp);
