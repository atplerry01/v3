namespace Whycespace.Engines.T0U.Governance.Commands;

public sealed record RevokeEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    string RevokedByGuardianId,
    string Reason,
    DateTime Timestamp);
