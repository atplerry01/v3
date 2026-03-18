namespace Whycespace.Engines.T3I.Reporting.Workforce.Models;

public enum AuditActionType
{
    WorkforceRegistered,
    CapabilityAssigned,
    AvailabilityChanged,
    AssignmentCreated,
    ScheduleCreated,
    LifecycleChanged,
    IncentiveEvaluated
}

public sealed record WorkforceAuditCommand(
    Guid WorkforceId,
    AuditActionType ActionType,
    Guid ActionReferenceId,
    Guid PerformedBy,
    DateTimeOffset Timestamp,
    string Details
);
