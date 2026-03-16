namespace Whycespace.Systems.Midstream.WSS.Configuration;

public sealed record WorkflowConfiguration(
    string WorkflowName,
    int MaxRetries,
    TimeSpan Timeout,
    bool RequiresPolicy
);

public sealed class WorkflowConfigurationStore
{
    private readonly Dictionary<string, WorkflowConfiguration> _configs = new();

    public void Register(WorkflowConfiguration config)
    {
        _configs[config.WorkflowName] = config;
    }

    public WorkflowConfiguration? Get(string workflowName)
    {
        _configs.TryGetValue(workflowName, out var config);
        return config;
    }
}
