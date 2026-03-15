namespace Whycespace.Engines.T0U.Governance.Commands;

public sealed record ResolveGovernanceDisputeCommand(
    Guid CommandId,
    Guid DisputeId,
    string ResolutionOutcome,
    Guid ResolvedByGuardianId,
    DateTime Timestamp);
