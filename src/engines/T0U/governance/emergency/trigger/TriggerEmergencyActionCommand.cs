namespace Whycespace.Engines.T0U.Governance.Emergency.Trigger;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record TriggerEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    EmergencyType EmergencyType,
    string TargetDomain,
    string TriggeredByGuardianId,
    string Reason,
    DateTime Timestamp);
