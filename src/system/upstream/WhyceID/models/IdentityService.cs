namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityService(
    Guid ServiceId,
    string Name,
    string Type,
    string Secret,
    DateTime CreatedAt,
    bool Revoked
);
