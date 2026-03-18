using Whycespace.Engines.T0U.WhyceID;
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
