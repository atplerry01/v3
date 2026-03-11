namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityRecovery(
    Guid RecoveryId,
    Guid IdentityId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
