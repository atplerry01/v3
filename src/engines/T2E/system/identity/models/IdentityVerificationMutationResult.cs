namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record IdentityVerificationMutationResult(
    Guid IdentityId,
    string VerificationType,
    string Status,
    Guid ExecutedBy,
    DateTime ExecutedAt);
