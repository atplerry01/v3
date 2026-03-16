using Whycespace.Engines.T0U.WhyceID;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class DeviceTrustEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityDeviceStore _store;
    private readonly DeviceTrustEngine _engine;

    public DeviceTrustEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityDeviceStore();
        _engine = new DeviceTrustEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void RegisterDevice_ShouldSucceed()
    {
        var identityId = RegisterIdentity();

        _engine.RegisterDevice(identityId, "fingerprint-abc123");

        var devices = _engine.GetDevices(identityId);
        Assert.Single(devices);
        Assert.Equal("fingerprint-abc123", devices.First().Fingerprint);
        Assert.False(devices.First().Trusted);
    }

    [Fact]
    public void RegisterDevice_MissingIdentity_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterDevice(Guid.NewGuid(), "fingerprint-abc123"));
    }

    [Fact]
    public void RegisterDevice_EmptyFingerprint_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.RegisterDevice(identityId, ""));
    }

    [Fact]
    public void TrustDevice_ShouldMarkAsTrusted()
    {
        var identityId = RegisterIdentity();
        _engine.RegisterDevice(identityId, "fingerprint-abc123");
        var deviceId = _engine.GetDevices(identityId).First().DeviceId;

        _engine.TrustDevice(identityId, deviceId);

        Assert.True(_engine.IsTrusted(identityId, deviceId));
    }

    [Fact]
    public void IsTrusted_UntrustedDevice_ShouldReturnFalse()
    {
        var identityId = RegisterIdentity();
        _engine.RegisterDevice(identityId, "fingerprint-abc123");
        var deviceId = _engine.GetDevices(identityId).First().DeviceId;

        Assert.False(_engine.IsTrusted(identityId, deviceId));
    }

    [Fact]
    public void GetDevices_ShouldReturnAllDevices()
    {
        var identityId = RegisterIdentity();
        _engine.RegisterDevice(identityId, "fingerprint-1");
        _engine.RegisterDevice(identityId, "fingerprint-2");
        _engine.RegisterDevice(identityId, "fingerprint-3");

        var devices = _engine.GetDevices(identityId);
        Assert.Equal(3, devices.Count);
    }

    [Fact]
    public void GetDevices_UnknownIdentity_ShouldReturnEmpty()
    {
        var devices = _engine.GetDevices(Guid.NewGuid());
        Assert.Empty(devices);
    }

    [Fact]
    public void RegisterDevice_DuplicateFingerprint_ShouldBeAllowed()
    {
        var identityId = RegisterIdentity();
        _engine.RegisterDevice(identityId, "fingerprint-same");
        _engine.RegisterDevice(identityId, "fingerprint-same");

        var devices = _engine.GetDevices(identityId);
        Assert.Equal(2, devices.Count);
    }
}
