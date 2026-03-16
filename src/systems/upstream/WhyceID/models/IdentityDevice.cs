namespace Whycespace.Systems.WhyceID.Models;

public sealed record IdentityDevice(
    Guid DeviceId,
    string Fingerprint,
    bool Trusted,
    DateTime RegisteredAt);
