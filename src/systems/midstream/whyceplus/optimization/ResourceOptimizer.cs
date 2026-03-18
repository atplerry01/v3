namespace Whycespace.Systems.Midstream.WhycePlus.Optimization;

public sealed class ResourceOptimizer
{
    private readonly List<OptimizationResult> _history = new();

    public OptimizationResult Optimize(string resourceType, decimal currentUtilization, decimal targetUtilization)
    {
        var recommendation = currentUtilization > targetUtilization
            ? OptimizationAction.ScaleDown
            : currentUtilization < targetUtilization * 0.5m
                ? OptimizationAction.ScaleUp
                : OptimizationAction.Maintain;

        var result = new OptimizationResult(
            Guid.NewGuid().ToString(), resourceType, currentUtilization,
            targetUtilization, recommendation, DateTimeOffset.UtcNow);

        _history.Add(result);
        return result;
    }

    public IReadOnlyList<OptimizationResult> GetHistory() => _history;
}

public sealed record OptimizationResult(
    string ResultId,
    string ResourceType,
    decimal CurrentUtilization,
    decimal TargetUtilization,
    OptimizationAction Recommendation,
    DateTimeOffset EvaluatedAt
);

public enum OptimizationAction
{
    Maintain,
    ScaleUp,
    ScaleDown,
    Redistribute
}
