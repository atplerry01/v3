namespace Whycespace.Systems.WhyceID.Models;

public sealed record IdentityRevocation(
    Guid RevocationId,
    Guid IdentityId,
    string Reason,
    DateTime CreatedAt,
    bool Active
);
