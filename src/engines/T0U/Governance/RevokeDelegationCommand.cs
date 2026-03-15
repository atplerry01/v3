namespace Whycespace.Engines.T0U.Governance;

public sealed record RevokeDelegationCommand(
    Guid CommandId,
    string DelegationId,
    string RequestedBy,
    string Reason,
    DateTime Timestamp);
