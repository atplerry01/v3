namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record AuthorizeActionCommand(
    Guid IdentityId,
    string ResourceType,
    Guid ResourceId,
    string Action,
    string RequiredPermission,
    string AccessScope,
    double TrustScore,
    double DeviceTrustScore,
    DateTime Timestamp);
