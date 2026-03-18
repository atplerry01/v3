namespace Whycespace.Engines.T3I.Reporting.Identity.Models;

public enum IdentityAuditAction
{
    IdentityCreated,
    IdentityVerified,
    RoleAssigned,
    PermissionGranted,
    DeviceRegistered,
    SessionCreated,
    RecoveryRequested,
    RevocationExecuted,
    TrustScoreUpdated,
    PolicyDecisionApplied
}

public sealed record IdentityAuditCommand(
    Guid IdentityId,
    IdentityAuditAction AuditAction,
    string SourceSystem,
    Guid PerformedBy,
    Guid OperationReferenceId,
    string Metadata,
    DateTimeOffset Timestamp
);
