namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record RevokeDelegationCommand(
    Guid CommandId,
    string DelegationId,
    string RequestedBy,
    string Reason,
    DateTime Timestamp);
