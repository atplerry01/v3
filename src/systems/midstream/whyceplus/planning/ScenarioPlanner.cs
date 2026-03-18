namespace Whycespace.Systems.Midstream.WhycePlus.Planning;

public sealed class ScenarioPlanner
{
    private readonly List<PlanningScenario> _scenarios = new();

    public PlanningScenario CreateScenario(string name, string scope, IReadOnlyDictionary<string, object> parameters)
    {
        var scenario = new PlanningScenario(
            Guid.NewGuid().ToString(), name, scope, parameters, ScenarioStatus.Created, DateTimeOffset.UtcNow);
        _scenarios.Add(scenario);
        return scenario;
    }

    public IReadOnlyList<PlanningScenario> GetScenarios(string? scope = null)
    {
        return scope is null
            ? _scenarios
            : _scenarios.Where(s => s.Scope == scope).ToList();
    }
}

public sealed record PlanningScenario(
    string ScenarioId,
    string Name,
    string Scope,
    IReadOnlyDictionary<string, object> Parameters,
    ScenarioStatus Status,
    DateTimeOffset CreatedAt
);

public enum ScenarioStatus
{
    Created,
    Running,
    Completed,
    Failed
}
