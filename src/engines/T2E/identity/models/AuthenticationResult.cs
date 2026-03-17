namespace Whycespace.Engines.T2E.Identity.Models;

public sealed record AuthenticationResult(
    Guid IdentityId,
    bool Authenticated,
    string AuthenticationMethod,
    double DeviceTrustScore,
    string FailureReason,
    DateTime AuthenticatedAt);
