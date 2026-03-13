namespace Whycespace.ReliabilityRuntime.Idempotency;

public sealed class IdempotencyGuard
{
    private readonly DuplicateExecutionRegistry _registry;

    public IdempotencyGuard(DuplicateExecutionRegistry registry)
    {
        _registry = registry;
    }

    public bool AllowExecution(string executionId)
    {
        return _registry.Register(executionId);
    }
}
