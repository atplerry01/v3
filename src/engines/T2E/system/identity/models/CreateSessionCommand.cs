namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record CreateSessionCommand(
    Guid IdentityId,
    string DeviceId,
    string AuthenticationMethod,
    double DeviceTrustScore,
    string IpAddress,
    string GeoLocation,
    DateTime Timestamp);
