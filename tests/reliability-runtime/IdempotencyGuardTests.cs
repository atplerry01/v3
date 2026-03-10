using Whycespace.ReliabilityRuntime.Idempotency;

namespace Whycespace.ReliabilityRuntime.Tests;

public sealed class IdempotencyGuardTests
{
    [Fact]
    public void AllowExecution_ReturnsTrue_ForFirstExecution()
    {
        var registry = new DuplicateExecutionRegistry();
        var guard = new IdempotencyGuard(registry);

        Assert.True(guard.AllowExecution("exec-1"));
    }

    [Fact]
    public void AllowExecution_ReturnsFalse_ForDuplicate()
    {
        var registry = new DuplicateExecutionRegistry();
        var guard = new IdempotencyGuard(registry);

        guard.AllowExecution("exec-1");

        Assert.False(guard.AllowExecution("exec-1"));
    }
}
