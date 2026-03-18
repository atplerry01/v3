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
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class ServiceIdentityEngineTests
{
    private readonly IdentityServiceStore _store;
    private readonly ServiceIdentityEngine _engine;

    public ServiceIdentityEngineTests()
    {
        _store = new IdentityServiceStore();
        _engine = new ServiceIdentityEngine(_store);
    }

    [Fact]
    public void RegisterService_ValidInput_ReturnsService()
    {
        var service = _engine.RegisterService("wss-runtime", "workflow-engine", "secret-key-123");

        Assert.NotEqual(Guid.Empty, service.ServiceId);
        Assert.Equal("wss-runtime", service.Name);
        Assert.Equal("workflow-engine", service.Type);
        Assert.Equal("secret-key-123", service.Secret);
        Assert.False(service.Revoked);
    }

    [Fact]
    public void RegisterService_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => _engine.RegisterService("", "workflow-engine", "secret"));
    }

    [Fact]
    public void RegisterService_EmptyType_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => _engine.RegisterService("wss-runtime", "", "secret"));
    }

    [Fact]
    public void RegisterService_EmptySecret_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => _engine.RegisterService("wss-runtime", "workflow-engine", ""));
    }

    [Fact]
    public void AuthenticateService_ValidCredentials_ReturnsTrue()
    {
        var service = _engine.RegisterService("wss-runtime", "workflow-engine", "secret-key");

        var result = _engine.AuthenticateService(service.ServiceId, "secret-key");

        Assert.True(result);
    }

    [Fact]
    public void AuthenticateService_WrongSecret_ReturnsFalse()
    {
        var service = _engine.RegisterService("wss-runtime", "workflow-engine", "secret-key");

        var result = _engine.AuthenticateService(service.ServiceId, "wrong-secret");

        Assert.False(result);
    }

    [Fact]
    public void AuthenticateService_RevokedService_ReturnsFalse()
    {
        var service = _engine.RegisterService("wss-runtime", "workflow-engine", "secret-key");
        _engine.RevokeService(service.ServiceId);

        var result = _engine.AuthenticateService(service.ServiceId, "secret-key");

        Assert.False(result);
    }

    [Fact]
    public void GetServices_ReturnsAllRegistered()
    {
        _engine.RegisterService("service-a", "type-a", "secret-a");
        _engine.RegisterService("service-b", "type-b", "secret-b");
        _engine.RegisterService("service-c", "type-c", "secret-c");

        var services = _engine.GetServices();

        Assert.Equal(3, services.Count);
    }
}
