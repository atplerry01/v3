namespace Whycespace.Tests.Workflows;

using Whycespace.Systems.Midstream.WSS.Workflows;
using Xunit;

public sealed class EconomicLifecycleWorkflowTests
{
    [Fact]
    public void BuildGraph_FollowsEconomicLifecycle()
    {
        var workflow = new EconomicLifecycleWorkflow();
        var graph = workflow.BuildGraph();

        Assert.Equal("EconomicLifecycle", workflow.WorkflowName);
        Assert.Equal(4, graph.Steps.Count);
        Assert.Equal("AllocateCapital", graph.Steps[0].StepId);
        Assert.Equal("CreateSpv", graph.Steps[1].StepId);
        Assert.Equal("RecordRevenue", graph.Steps[2].StepId);
        Assert.Equal("DistributeProfit", graph.Steps[3].StepId);
    }
}
