namespace Whycespace.System.WhyceID.Models;

public sealed record IdentitySession(
    Guid SessionId,
    Guid IdentityId,
    Guid DeviceId,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool Active);
