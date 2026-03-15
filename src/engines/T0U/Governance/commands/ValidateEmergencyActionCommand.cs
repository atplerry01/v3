namespace Whycespace.Engines.T0U.Governance.Commands;

public sealed record ValidateEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    string GuardianId,
    DateTime Timestamp);
