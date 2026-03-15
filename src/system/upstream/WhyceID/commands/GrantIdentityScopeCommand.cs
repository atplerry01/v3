namespace Whycespace.System.WhyceID.Commands;

public sealed record GrantIdentityScopeCommand(
    Guid IdentityId,
    string ScopeKey,
    Guid GrantedBy,
    DateTime Timestamp);
