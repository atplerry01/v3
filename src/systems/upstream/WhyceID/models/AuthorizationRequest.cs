namespace Whycespace.Systems.WhyceID.Models;

public sealed record AuthorizationRequest(
    Guid IdentityId,
    string Resource,
    string Action,
    string Scope);
