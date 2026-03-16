namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record ResolveGovernanceDisputeCommand(
    Guid CommandId,
    Guid DisputeId,
    string ResolutionOutcome,
    Guid ResolvedByGuardianId,
    DateTime Timestamp);
