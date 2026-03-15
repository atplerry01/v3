namespace Whycespace.Engines.T2E.Core.Identity.Models;

public sealed record AuthenticateIdentityCommand(
    Guid IdentityId,
    string CredentialType,
    string CredentialValue,
    string DeviceId,
    string DeviceFingerprint,
    string IpAddress,
    string GeoLocation,
    string AuthenticationMethod,
    DateTime Timestamp);
