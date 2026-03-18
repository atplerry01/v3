namespace Whycespace.CommandSystem.Tests;

using Whycespace.CommandSystem.Routing;

public class CommandRouterTests
{
    private readonly CommandRouter _router = new();

    [Fact]
    public void ResolveWorkflow_UnmappedCommand_ReturnsNull()
    {
        Assert.Null(_router.ResolveWorkflow("UnknownCommand"));
    }

    [Fact]
    public void MapCommand_ThenResolve_ReturnsWorkflowName()
    {
        _router.MapCommand("RequestRideCommand", "RideRequestWorkflow");
        Assert.Equal("RideRequestWorkflow", _router.ResolveWorkflow("RequestRideCommand"));
    }

    [Fact]
    public void MapCommand_OverwritesExisting()
    {
        _router.MapCommand("TestCommand", "WorkflowA");
        _router.MapCommand("TestCommand", "WorkflowB");
        Assert.Equal("WorkflowB", _router.ResolveWorkflow("TestCommand"));
    }

    [Fact]
    public void GetRoutes_ReturnsAllMappings()
    {
        _router.MapCommand("CommandA", "WorkflowA");
        _router.MapCommand("CommandB", "WorkflowB");

        var routes = _router.GetRoutes();
        Assert.Equal(2, routes.Count);
        Assert.Equal("WorkflowA", routes["CommandA"]);
        Assert.Equal("WorkflowB", routes["CommandB"]);
    }
}
