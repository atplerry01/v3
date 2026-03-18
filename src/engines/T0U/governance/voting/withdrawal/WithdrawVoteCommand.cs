namespace Whycespace.Engines.T0U.Governance.Voting.Withdrawal;

public sealed record WithdrawVoteCommand(
    string CommandId,
    string ProposalId,
    string GuardianId,
    string Reason,
    DateTime Timestamp);
