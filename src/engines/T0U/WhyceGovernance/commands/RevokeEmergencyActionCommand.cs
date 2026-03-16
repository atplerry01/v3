namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record RevokeEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    string RevokedByGuardianId,
    string Reason,
    DateTime Timestamp);
