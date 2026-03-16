namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record WithdrawVoteCommand(
    string CommandId,
    string ProposalId,
    string GuardianId,
    string Reason,
    DateTime Timestamp);
