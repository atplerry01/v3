namespace Whycespace.System.WhyceID.Models;

public sealed record AuthorizationRequest(
    Guid IdentityId,
    string Resource,
    string Action,
    string Scope);
