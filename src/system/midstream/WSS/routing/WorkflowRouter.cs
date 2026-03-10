namespace Whycespace.System.Midstream.WSS.Routing;

using Whycespace.Contracts.Commands;

public sealed class WorkflowRouter
{
    private readonly Dictionary<Type, string> _routes = new();

    public void MapCommand<TCommand>(string workflowName) where TCommand : ICommand
    {
        _routes[typeof(TCommand)] = workflowName;
    }

    public string? ResolveWorkflow(ICommand command)
    {
        _routes.TryGetValue(command.GetType(), out var workflowName);
        return workflowName;
    }
}
