namespace Whycespace.RuntimeDispatcher.Tests;

using Whycespace.RuntimeDispatcher.Resolver;

public class WorkflowResolverTests
{
    private readonly WorkflowResolver _resolver = new();

    [Fact]
    public void ResolveWorkflow_RequestRideCommand_ReturnsRideRequestWorkflow()
    {
        var result = _resolver.ResolveWorkflow("RequestRideCommand");
        Assert.Equal("RideRequestWorkflow", result);
    }

    [Fact]
    public void ResolveWorkflow_ListPropertyCommand_ReturnsPropertyListingWorkflow()
    {
        var result = _resolver.ResolveWorkflow("ListPropertyCommand");
        Assert.Equal("PropertyListingWorkflow", result);
    }

    [Fact]
    public void ResolveWorkflow_AllocateCapitalCommand_ReturnsEconomicLifecycleWorkflow()
    {
        var result = _resolver.ResolveWorkflow("AllocateCapitalCommand");
        Assert.Equal("EconomicLifecycleWorkflow", result);
    }

    [Fact]
    public void ResolveWorkflow_UnknownCommand_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveWorkflow("UnknownCommand"));
        Assert.Contains("No workflow mapped", ex.Message);
    }

    [Fact]
    public void MapCommand_AddsNewMapping()
    {
        _resolver.MapCommand("CreateSpvCommand", "SpvCreationWorkflow");
        var result = _resolver.ResolveWorkflow("CreateSpvCommand");
        Assert.Equal("SpvCreationWorkflow", result);
    }

    [Fact]
    public void GetMappings_ReturnsAllMappings()
    {
        var mappings = _resolver.GetMappings();
        Assert.Equal(3, mappings.Count);
        Assert.Contains("RequestRideCommand", mappings.Keys);
        Assert.Contains("ListPropertyCommand", mappings.Keys);
        Assert.Contains("AllocateCapitalCommand", mappings.Keys);
    }
}
