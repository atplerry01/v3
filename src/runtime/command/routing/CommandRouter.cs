namespace Whycespace.CommandSystem.Routing;

public sealed class CommandRouter : ICommandRouter
{
    private readonly Dictionary<string, string> _routes = new();

    public void MapCommand(string commandType, string workflowName)
    {
        _routes[commandType] = workflowName;
    }

    public string? ResolveWorkflow(string commandType)
    {
        _routes.TryGetValue(commandType, out var workflowName);
        return workflowName;
    }

    public IReadOnlyDictionary<string, string> GetRoutes()
    {
        return _routes.AsReadOnly();
    }
}
