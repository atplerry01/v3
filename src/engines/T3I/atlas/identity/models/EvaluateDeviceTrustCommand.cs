namespace Whycespace.Engines.T3I.Atlas.Identity.Models;

public sealed record EvaluateDeviceTrustCommand(
    Guid IdentityId,
    string DeviceId,
    string DeviceFingerprint,
    string DeviceType,
    string OperatingSystem,
    string IpAddress,
    string GeoLocation,
    int PreviousDeviceUsageCount,
    int DeviceAgeDays,
    DateTime Timestamp);
