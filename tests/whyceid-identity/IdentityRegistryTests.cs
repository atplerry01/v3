using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityRegistryTests
{
    [Fact]
    public void Register_ShouldStoreIdentity()
    {
        var registry = new IdentityRegistry();
        var identity = new IdentityAggregate(
            new IdentityId(Guid.NewGuid()),
            IdentityType.Individual,
            DateTime.UtcNow);

        registry.Register(identity);

        Assert.True(registry.Exists(identity.IdentityId.Value));
    }

    [Fact]
    public void Get_ShouldReturnRegisteredIdentity()
    {
        var registry = new IdentityRegistry();
        var id = Guid.NewGuid();
        var identity = new IdentityAggregate(
            new IdentityId(id),
            IdentityType.SystemService,
            DateTime.UtcNow);

        registry.Register(identity);

        var result = registry.Get(id);
        Assert.Equal(id, result.IdentityId.Value);
        Assert.Equal(IdentityType.SystemService, result.Type);
    }

    [Fact]
    public void Register_DuplicateId_ShouldThrow()
    {
        var registry = new IdentityRegistry();
        var id = Guid.NewGuid();
        var identity = new IdentityAggregate(
            new IdentityId(id),
            IdentityType.Individual,
            DateTime.UtcNow);

        registry.Register(identity);

        var duplicate = new IdentityAggregate(
            new IdentityId(id),
            IdentityType.Organization,
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => registry.Register(duplicate));
    }

    [Fact]
    public void Get_NotFound_ShouldThrow()
    {
        var registry = new IdentityRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.Get(Guid.NewGuid()));
    }

    [Fact]
    public void Exists_ShouldReturnFalse_WhenNotRegistered()
    {
        var registry = new IdentityRegistry();

        Assert.False(registry.Exists(Guid.NewGuid()));
    }
}
