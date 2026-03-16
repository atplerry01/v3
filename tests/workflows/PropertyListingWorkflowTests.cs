namespace Whycespace.Tests.Workflows;

using Whycespace.Systems.Midstream.WSS.Workflows;
using Xunit;

public sealed class PropertyListingWorkflowTests
{
    [Fact]
    public void BuildGraph_ReturnsValidGraph()
    {
        var workflow = new PropertyListingWorkflow();
        var graph = workflow.BuildGraph();

        Assert.Equal("PropertyListing", workflow.WorkflowName);
        Assert.NotEmpty(graph.Steps);
    }
}
