namespace Whycespace.Systems.WhyceID.Commands;

public sealed record RevokeIdentityScopeCommand(
    Guid IdentityId,
    string ScopeKey,
    Guid RevokedBy,
    DateTime Timestamp);
