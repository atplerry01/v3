namespace Whycespace.CommandSystem.Tests;

using Whycespace.CommandSystem.Idempotency;

public class IdempotencyRegistryTests
{
    private readonly InMemoryIdempotencyRegistry _registry = new();

    [Fact]
    public void Exists_UnknownId_ReturnsFalse()
    {
        Assert.False(_registry.Exists(Guid.NewGuid()));
    }

    [Fact]
    public void Register_ThenExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _registry.Register(id);
        Assert.True(_registry.Exists(id));
    }

    [Fact]
    public void Register_DuplicateId_DoesNotThrow()
    {
        var id = Guid.NewGuid();
        _registry.Register(id);
        _registry.Register(id);
        Assert.True(_registry.Exists(id));
    }
}
