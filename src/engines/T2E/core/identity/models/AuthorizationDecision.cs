namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record AuthorizationDecision(
    Guid IdentityId,
    bool Authorized,
    string PermissionGranted,
    bool ScopeValidated,
    string Reason,
    DateTime EvaluatedAt);
