using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityRegistryTests
{
    [Fact]
    public void Register_ShouldStoreIdentity()
    {
        var registry = new IdentityRegistry();
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.User);

        registry.Register(identity);

        Assert.True(registry.Exists(identity.Id.Value));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void Get_ShouldReturnRegisteredIdentity()
    {
        var registry = new IdentityRegistry();
        var id = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(id), IdentityType.System);

        registry.Register(identity);

        var result = registry.Get(id);
        Assert.Equal(id, result.Id.Value);
        Assert.Equal(IdentityType.System, result.Type);
    }

    [Fact]
    public void Register_DuplicateId_ShouldThrow()
    {
        var registry = new IdentityRegistry();
        var id = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(id), IdentityType.User);

        registry.Register(identity);

        var duplicate = new IdentityAggregate(IdentityId.From(id), IdentityType.Service);

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

    [Fact]
    public void Update_ShouldReplaceIdentity()
    {
        var registry = new IdentityRegistry();
        var id = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(id), IdentityType.User);

        registry.Register(identity);

        identity.Verify();
        registry.Update(identity);

        var result = registry.Get(id);
        Assert.Equal(IdentityStatus.Verified, result.Status);
    }

    [Fact]
    public void GetAll_ShouldReturnAllRegisteredIdentities()
    {
        var registry = new IdentityRegistry();

        var id1 = new IdentityAggregate(IdentityId.New(), IdentityType.User);
        var id2 = new IdentityAggregate(IdentityId.New(), IdentityType.Service);
        var id3 = new IdentityAggregate(IdentityId.New(), IdentityType.System);

        registry.Register(id1);
        registry.Register(id2);
        registry.Register(id3);

        var all = registry.GetAll();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void Register_NullIdentity_ShouldThrow()
    {
        var registry = new IdentityRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public async Task ConcurrentRegistration_ShouldBeSafe()
    {
        var registry = new IdentityRegistry();
        var tasks = new Task[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var identity = new IdentityAggregate(IdentityId.New(), IdentityType.User);
                registry.Register(identity);
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(100, registry.Count);
    }
}
