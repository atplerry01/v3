namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record CastVoteCommand(
    string CommandId,
    string ProposalId,
    string GuardianId,
    VoteType VoteDecision,
    int VoteWeight,
    DateTime Timestamp);
