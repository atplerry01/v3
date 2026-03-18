namespace Whycespace.Tests.Workflows;

using Whycespace.Systems.Midstream.WSS.Workflows;
using Xunit;

public sealed class RideRequestWorkflowTests
{
    [Fact]
    public void BuildGraph_ReturnsValidGraph()
    {
        var workflow = new RideRequestWorkflow();
        var graph = workflow.BuildGraph();

        Assert.Equal("RideRequest", workflow.WorkflowName);
        Assert.NotEmpty(graph.Steps);
        Assert.Equal("validate-policy", graph.Steps[0].StepId);
    }

    [Fact]
    public void BuildGraph_AllStepsHaveEngineNames()
    {
        var workflow = new RideRequestWorkflow();
        var graph = workflow.BuildGraph();

        foreach (var step in graph.Steps)
        {
            Assert.False(string.IsNullOrEmpty(step.EngineName));
        }
    }
}
