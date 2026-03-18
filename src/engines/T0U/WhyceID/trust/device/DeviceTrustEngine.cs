namespace Whycespace.Engines.T0U.WhyceID.Trust.Device;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class DeviceTrustEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityDeviceStore _store;

    public DeviceTrustEngine(
        IdentityRegistry registry,
        IdentityDeviceStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void RegisterDevice(Guid identityId, string fingerprint)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}");
        }

        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("Device fingerprint cannot be empty.");
        }

        var device = new IdentityDevice(
            Guid.NewGuid(),
            fingerprint,
            false,
            DateTime.UtcNow);

        _store.Register(identityId, device);
    }

    public void TrustDevice(Guid identityId, Guid deviceId)
    {
        _store.TrustDevice(identityId, deviceId);
    }

    public bool IsTrusted(Guid identityId, Guid deviceId)
    {
        return _store.IsTrusted(identityId, deviceId);
    }

    public IReadOnlyCollection<IdentityDevice> GetDevices(Guid identityId)
    {
        return _store.Get(identityId);
    }
}
