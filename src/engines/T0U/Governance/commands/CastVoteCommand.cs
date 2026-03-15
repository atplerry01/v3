namespace Whycespace.Engines.T0U.Governance.Commands;

using Whycespace.System.Upstream.Governance.Models;

public sealed record CastVoteCommand(
    string CommandId,
    string ProposalId,
    string GuardianId,
    VoteType VoteDecision,
    int VoteWeight,
    DateTime Timestamp);
