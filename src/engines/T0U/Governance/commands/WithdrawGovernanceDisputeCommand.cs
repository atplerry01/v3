namespace Whycespace.Engines.T0U.Governance.Commands;

public sealed record WithdrawGovernanceDisputeCommand(
    Guid CommandId,
    Guid DisputeId,
    Guid WithdrawnByGuardianId,
    string Reason,
    DateTime Timestamp);
