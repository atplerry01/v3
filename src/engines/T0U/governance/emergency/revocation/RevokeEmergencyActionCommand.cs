namespace Whycespace.Engines.T0U.Governance.Emergency.Revocation;

public sealed record RevokeEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    string RevokedByGuardianId,
    string Reason,
    DateTime Timestamp);
