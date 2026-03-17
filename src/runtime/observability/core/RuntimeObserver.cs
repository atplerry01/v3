namespace Whycespace.Runtime.Observability.Core;

public sealed record EngineInvocationLog(
    Guid InvocationId,
    string EngineName,
    string WorkflowId,
    bool Success,
    long DurationMs,
    DateTimeOffset Timestamp
);

public sealed class RuntimeObserver
{
    private readonly List<EngineInvocationLog> _logs = new();

    public void LogInvocation(EngineInvocationLog log) => _logs.Add(log);

    public IReadOnlyList<EngineInvocationLog> GetLogs() => _logs;

    public IReadOnlyList<EngineInvocationLog> GetLogsByEngine(string engineName)
        => _logs.Where(l => l.EngineName == engineName).ToList();
}
