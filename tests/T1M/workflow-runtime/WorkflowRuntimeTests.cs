namespace Whycespace.WorkflowRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public class WorkflowRuntimeTests
{
    private readonly WorkflowRegistry _registry = new();
    private readonly WfRuntime _runtime;

    public WorkflowRuntimeTests()
    {
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(EngineResult.Ok(
                Array.Empty<EngineEvent>(),
                new Dictionary<string, object> { ["executed"] = true })));

        _runtime = new WfRuntime(_registry, executor);

        var graph = new WorkflowGraph(
            Guid.NewGuid().ToString(),
            "TestWorkflow",
            new[] { new WorkflowStep("step-1", "Step 1", "TestEngine", Array.Empty<string>()) });
        _registry.Register(graph);
    }

    [Fact]
    public async Task ExecuteAsync_RegisteredWorkflow_ReturnsSuccess()
    {
        var request = new WorkflowExecutionRequest("TestWorkflow", new Dictionary<string, object>());
        var result = await _runtime.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.True((bool)result.Output["executed"]);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownWorkflow_Throws()
    {
        var request = new WorkflowExecutionRequest("NonExistent", new Dictionary<string, object>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _runtime.ExecuteAsync(request));
        Assert.Contains("Workflow not found", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_PassesContextToExecutor()
    {
        var input = new Dictionary<string, object> { ["key"] = "value" };
        var request = new WorkflowExecutionRequest("TestWorkflow", input);

        var result = await _runtime.ExecuteAsync(request);

        Assert.True(result.Success);
        Assert.Equal("value", result.Output["key"]);
    }
}
