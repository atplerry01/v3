namespace Whycespace.Runtime.ProjectionGovernance;

public sealed class ProjectionConsistencyRules
{
    private readonly Dictionary<string, ConsistencyLevel> _rules = new();

    public void SetConsistencyLevel(string projectionName, ConsistencyLevel level)
    {
        _rules[projectionName] = level;
    }

    public ConsistencyLevel GetConsistencyLevel(string projectionName)
    {
        return _rules.GetValueOrDefault(projectionName, ConsistencyLevel.Eventual);
    }

    public IReadOnlyDictionary<string, ConsistencyLevel> GetAllRules()
    {
        return _rules.AsReadOnly();
    }
}

public enum ConsistencyLevel
{
    Eventual,
    Strong,
    BoundedStaleness
}
