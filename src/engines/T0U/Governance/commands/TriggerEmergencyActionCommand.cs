namespace Whycespace.Engines.T0U.Governance.Commands;

using Whycespace.System.Upstream.Governance.Models;

public sealed record TriggerEmergencyActionCommand(
    Guid CommandId,
    string EmergencyActionId,
    EmergencyType EmergencyType,
    string TargetDomain,
    string TriggeredByGuardianId,
    string Reason,
    DateTime Timestamp);
