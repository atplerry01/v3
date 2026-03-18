namespace Whycespace.Engines.T0U.Governance.Dispute.Resolution;

public sealed record ResolveGovernanceDisputeCommand(
    Guid CommandId,
    Guid DisputeId,
    string ResolutionOutcome,
    Guid ResolvedByGuardianId,
    DateTime Timestamp);
