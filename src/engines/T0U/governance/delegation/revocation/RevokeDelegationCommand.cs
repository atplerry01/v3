namespace Whycespace.Engines.T0U.Governance.Delegation.Revocation;

public sealed record RevokeDelegationCommand(
    Guid CommandId,
    string DelegationId,
    string RequestedBy,
    string Reason,
    DateTime Timestamp);
