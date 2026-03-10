using Whycespace.ReliabilityRuntime.Idempotency;

namespace Whycespace.ReliabilityRuntime.Tests;

public sealed class DuplicateExecutionRegistryTests
{
    [Fact]
    public void Register_ReturnsTrue_ForNewExecution()
    {
        var registry = new DuplicateExecutionRegistry();

        Assert.True(registry.Register("exec-1"));
    }

    [Fact]
    public void Register_ReturnsFalse_ForDuplicate()
    {
        var registry = new DuplicateExecutionRegistry();
        registry.Register("exec-1");

        Assert.False(registry.Register("exec-1"));
    }

    [Fact]
    public void Exists_ReturnsTrue_WhenRegistered()
    {
        var registry = new DuplicateExecutionRegistry();
        registry.Register("exec-1");

        Assert.True(registry.Exists("exec-1"));
    }

    [Fact]
    public void Exists_ReturnsFalse_WhenNotRegistered()
    {
        var registry = new DuplicateExecutionRegistry();

        Assert.False(registry.Exists("exec-1"));
    }
}
