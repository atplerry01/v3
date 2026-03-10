namespace Whycespace.ReliabilityRuntime.Idempotency;

public sealed class DuplicateExecutionRegistry
{
    private readonly HashSet<string> _executions = new();

    public bool Register(string executionId)
    {
        return _executions.Add(executionId);
    }

    public bool Exists(string executionId)
    {
        return _executions.Contains(executionId);
    }
}
