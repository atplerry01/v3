namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record ValidateEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    string GuardianId,
    DateTime Timestamp);
