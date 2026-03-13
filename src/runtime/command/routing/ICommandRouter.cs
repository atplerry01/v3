namespace Whycespace.CommandSystem.Routing;

public interface ICommandRouter
{
    void MapCommand(string commandType, string workflowName);
    string? ResolveWorkflow(string commandType);
    IReadOnlyDictionary<string, string> GetRoutes();
}
