namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record IdentityVerificationMutationResult(
    Guid IdentityId,
    string VerificationType,
    string Status,
    Guid ExecutedBy,
    DateTime ExecutedAt);
