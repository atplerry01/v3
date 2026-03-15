namespace Whycespace.RuntimeDispatcher.Resolver;

using global::System.Collections.Concurrent;

public sealed class WorkflowResolver : IWorkflowResolver
{
    private readonly ConcurrentDictionary<string, string> _mappings = new();

    public WorkflowResolver()
    {
        _mappings["RequestRideCommand"] = "RideRequestWorkflow";
        _mappings["ListPropertyCommand"] = "PropertyListingWorkflow";
        _mappings["AllocateCapitalCommand"] = "EconomicLifecycleWorkflow";
    }

    public string ResolveWorkflow(string commandType)
    {
        if (_mappings.TryGetValue(commandType, out var workflowName))
            return workflowName;

        throw new InvalidOperationException($"No workflow mapped for command type: {commandType}");
    }

    public void MapCommand(string commandType, string workflowName)
    {
        _mappings[commandType] = workflowName;
    }

    public IReadOnlyDictionary<string, string> GetMappings() => _mappings;
}
