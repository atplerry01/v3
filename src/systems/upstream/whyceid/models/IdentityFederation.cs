namespace Whycespace.Systems.WhyceID.Models;

public sealed record IdentityFederation(
    Guid FederationId,
    string Provider,
    string ExternalIdentityId,
    Guid InternalIdentityId,
    DateTime CreatedAt,
    bool Revoked
);
