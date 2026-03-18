namespace Whycespace.Engines.T0U.Governance.Emergency.Validation;

public sealed record ValidateEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    string GuardianId,
    DateTime Timestamp);
