namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record WithdrawGovernanceDisputeCommand(
    Guid CommandId,
    Guid DisputeId,
    Guid WithdrawnByGuardianId,
    string Reason,
    DateTime Timestamp);
