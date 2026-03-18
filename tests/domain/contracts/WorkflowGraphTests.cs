namespace Whycespace.Contracts.Tests;

using Whycespace.Contracts.Workflows;

public sealed class WorkflowGraphTests
{
    [Fact]
    public void WorkflowGraph_Contains_Steps()
    {
        var steps = new[]
        {
            new WorkflowStep("step-1", "Validate", "PolicyValidation", new[] { "step-2" }),
            new WorkflowStep("step-2", "Execute", "RideExecution", Array.Empty<string>())
        };

        var graph = new WorkflowGraph("wf-1", "TestWorkflow", steps);

        Assert.Equal("wf-1", graph.WorkflowId);
        Assert.Equal("TestWorkflow", graph.Name);
        Assert.Equal(2, graph.Steps.Count);
    }

    [Fact]
    public void WorkflowStep_Has_Engine_Reference()
    {
        var step = new WorkflowStep("step-1", "Validate", "PolicyValidation", Array.Empty<string>());

        Assert.Equal("step-1", step.StepId);
        Assert.Equal("PolicyValidation", step.EngineName);
    }

    [Fact]
    public void WorkflowState_Tracks_Status()
    {
        var state = new WorkflowState(
            "wf-1", "step-1", WorkflowStatus.Running,
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow, null);

        Assert.Equal(WorkflowStatus.Running, state.Status);
        Assert.Null(state.CompletedAt);
    }

    [Fact]
    public void WorkflowContext_Contains_Data()
    {
        var data = new Dictionary<string, object> { ["userId"] = "u-1" };
        var context = new WorkflowContext("wf-1", "RideRequest", "step-1", data, DateTimeOffset.UtcNow);

        Assert.Equal("wf-1", context.WorkflowId);
        Assert.Equal("RideRequest", context.WorkflowName);
        Assert.Equal("u-1", context.Data["userId"]);
    }

    [Fact]
    public void WorkflowStatus_Has_All_States()
    {
        var values = Enum.GetValues<WorkflowStatus>();
        Assert.Contains(WorkflowStatus.Pending, values);
        Assert.Contains(WorkflowStatus.Running, values);
        Assert.Contains(WorkflowStatus.Completed, values);
        Assert.Contains(WorkflowStatus.Failed, values);
        Assert.Contains(WorkflowStatus.Cancelled, values);
    }
}
