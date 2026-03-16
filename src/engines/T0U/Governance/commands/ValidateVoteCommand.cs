namespace Whycespace.Engines.T0U.Governance.Commands;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record ValidateVoteCommand(
    string CommandId,
    string ProposalId,
    string GuardianId,
    VoteType VoteDecision,
    DateTime Timestamp);
