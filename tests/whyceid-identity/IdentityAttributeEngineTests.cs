using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityAttributeEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityAttributeStore _store;
    private readonly IdentityAttributeEngine _engine;

    public IdentityAttributeEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityAttributeStore();
        _engine = new IdentityAttributeEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void AddAttribute_ShouldSucceed()
    {
        var identityId = RegisterIdentity();

        _engine.AddAttribute(identityId, "email", "user@example.com");

        var attributes = _engine.GetAttributes(identityId);
        Assert.Single(attributes);
        Assert.Equal("email", attributes[0].Key);
        Assert.Equal("user@example.com", attributes[0].Value);
    }

    [Fact]
    public void AddAttribute_IdentityNotFound_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.AddAttribute(Guid.NewGuid(), "email", "test@example.com"));
    }

    [Fact]
    public void AddAttribute_EmptyKey_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.AddAttribute(identityId, "", "value"));
    }

    [Fact]
    public void AddAttribute_WhitespaceKey_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.AddAttribute(identityId, "  ", "value"));
    }

    [Fact]
    public void AddAttribute_EmptyValue_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.AddAttribute(identityId, "email", ""));
    }

    [Fact]
    public void GetAttributes_ShouldReturnEmptyForUnknownIdentity()
    {
        var attributes = _engine.GetAttributes(Guid.NewGuid());

        Assert.Empty(attributes);
    }

    [Fact]
    public void AddMultipleAttributes_ShouldBeRetrievable()
    {
        var identityId = RegisterIdentity();

        _engine.AddAttribute(identityId, "email", "user@example.com");
        _engine.AddAttribute(identityId, "country", "US");
        _engine.AddAttribute(identityId, "kyc_level", "3");

        var attributes = _engine.GetAttributes(identityId);
        Assert.Equal(3, attributes.Count);
    }
}
