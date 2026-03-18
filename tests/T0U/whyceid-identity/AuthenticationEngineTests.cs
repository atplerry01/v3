using Whycespace.Engines.T0U.WhyceID.Identity.Creation;
using Whycespace.Engines.T0U.WhyceID.Identity.Attributes;
using Whycespace.Engines.T0U.WhyceID.Identity.Graph;
using Whycespace.Engines.T0U.WhyceID.Authentication;
using Whycespace.Engines.T0U.WhyceID.Authorization.Decision;
using Whycespace.Engines.T0U.WhyceID.Consent;
using Whycespace.Engines.T0U.WhyceID.Trust.Device;
using Whycespace.Engines.T0U.WhyceID.Trust.Scoring;
using Whycespace.Engines.T0U.WhyceID.Federation.Provider;
using Whycespace.Engines.T0U.WhyceID.AccessScope.Assignment;
using Whycespace.Engines.T0U.WhyceID.Audit.Reporting;
using Whycespace.Engines.T0U.WhyceID.Recovery.Execution;
using Whycespace.Engines.T0U.WhyceID.Revocation.Execution;
using Whycespace.Engines.T0U.WhyceID.Roles.Assignment;
using Whycespace.Engines.T0U.WhyceID.Permissions.Grant;
using Whycespace.Engines.T0U.WhyceID.Policy.Enforcement;
using Whycespace.Engines.T0U.WhyceID.Verification.Identity;
using Whycespace.Engines.T0U.WhyceID.Service.Registration;
using Whycespace.Engines.T0U.WhyceID.Session.Creation;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class AuthenticationEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityTrustStore _trustStore;
    private readonly IdentityDeviceStore _deviceStore;
    private readonly TrustScoreEngine _trustEngine;
    private readonly DeviceTrustEngine _deviceEngine;
    private readonly AuthenticationEngine _engine;

    public AuthenticationEngineTests()
    {
        _registry = new IdentityRegistry();
        _trustStore = new IdentityTrustStore();
        _deviceStore = new IdentityDeviceStore();
        _trustEngine = new TrustScoreEngine(_registry, _trustStore);
        _deviceEngine = new DeviceTrustEngine(_registry, _deviceStore);
        _engine = new AuthenticationEngine(_registry, _trustEngine, _deviceEngine);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    private (Guid identityId, Guid deviceId) SetupAuthenticatedIdentity()
    {
        var identityId = RegisterIdentity();

        // Verify identity
        var identity = _registry.Get(identityId);
        identity.Verify();
        _registry.Update(identity);

        // Register and trust device
        _deviceEngine.RegisterDevice(identityId, "fingerprint-trusted");
        var deviceId = _deviceEngine.GetDevices(identityId).First().DeviceId;
        _deviceEngine.TrustDevice(identityId, deviceId);

        // Calculate trust score (verified = 50)
        _trustEngine.Calculate(identityId);

        return (identityId, deviceId);
    }

    [Fact]
    public void Authenticate_ShouldSucceed_WhenAllConditionsMet()
    {
        var (identityId, deviceId) = SetupAuthenticatedIdentity();

        var result = _engine.Authenticate(identityId, deviceId);

        Assert.True(result.Success);
        Assert.Equal("Authentication successful", result.Message);
    }

    [Fact]
    public void Authenticate_MissingIdentity_ShouldFail()
    {
        var result = _engine.Authenticate(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Identity does not exist", result.Message);
    }

    [Fact]
    public void Authenticate_UnverifiedIdentity_ShouldFail()
    {
        var identityId = RegisterIdentity();

        var result = _engine.Authenticate(identityId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Identity not verified", result.Message);
    }

    [Fact]
    public void Authenticate_UntrustedDevice_ShouldFail()
    {
        var identityId = RegisterIdentity();
        var identity = _registry.Get(identityId);
        identity.Verify();
        _registry.Update(identity);

        var result = _engine.Authenticate(identityId, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Device not trusted", result.Message);
    }

    [Fact]
    public void Authenticate_LowTrustScore_ShouldFail()
    {
        var identityId = RegisterIdentity();
        var identity = _registry.Get(identityId);
        identity.Verify();
        _registry.Update(identity);

        // Register and trust device
        _deviceEngine.RegisterDevice(identityId, "fingerprint-test");
        var deviceId = _deviceEngine.GetDevices(identityId).First().DeviceId;
        _deviceEngine.TrustDevice(identityId, deviceId);

        // No trust score calculated — should fail
        var result = _engine.Authenticate(identityId, deviceId);

        Assert.False(result.Success);
        Assert.Equal("Trust score too low", result.Message);
    }

    [Fact]
    public void Authenticate_AllConditionsMet_ShouldReturnSuccess()
    {
        var (identityId, deviceId) = SetupAuthenticatedIdentity();

        var result = _engine.Authenticate(identityId, deviceId);

        Assert.True(result.Success);
    }
}
