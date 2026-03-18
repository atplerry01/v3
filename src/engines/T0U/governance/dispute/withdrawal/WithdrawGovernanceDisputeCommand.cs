namespace Whycespace.Engines.T0U.Governance.Dispute.Withdrawal;

public sealed record WithdrawGovernanceDisputeCommand(
    Guid CommandId,
    Guid DisputeId,
    Guid WithdrawnByGuardianId,
    string Reason,
    DateTime Timestamp);
