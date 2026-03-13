namespace Whycespace.WorkflowRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.WorkflowRuntime.Executor;

public class WorkflowExecutorTests
{
    private static WorkflowGraph CreateGraph(params string[] engineNames)
    {
        var steps = engineNames.Select((name, i) =>
            new WorkflowStep($"step-{i}", $"Step {i}", name, Array.Empty<string>()))
            .ToList();

        return new WorkflowGraph(Guid.NewGuid().ToString(), "TestWorkflow", steps);
    }

    [Fact]
    public async Task ExecuteAsync_AllStepsSucceed_ReturnsSuccess()
    {
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(EngineResult.Ok(
                Array.Empty<EngineEvent>(),
                new Dictionary<string, object> { ["done"] = true })));

        var graph = CreateGraph("Engine1", "Engine2");

        var result = await executor.ExecuteAsync(graph, new Dictionary<string, object>());

        Assert.True(result.Success);
        Assert.True((bool)result.Output["done"]);
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_ReturnsFailure()
    {
        var callCount = 0;
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
        {
            callCount++;
            return callCount == 1
                ? Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>()))
                : Task.FromResult(EngineResult.Fail("Engine error"));
        });

        var graph = CreateGraph("Engine1", "Engine2");

        var result = await executor.ExecuteAsync(graph, new Dictionary<string, object>());

        Assert.False(result.Success);
        Assert.Contains("Engine error", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ContextFlowsBetweenSteps()
    {
        var stepIndex = 0;
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
        {
            stepIndex++;
            var output = new Dictionary<string, object>
            {
                [$"step{stepIndex}_result"] = $"value{stepIndex}"
            };
            return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), output));
        });

        var graph = CreateGraph("Engine1", "Engine2");

        var result = await executor.ExecuteAsync(graph, new Dictionary<string, object> { ["input"] = "initial" });

        Assert.True(result.Success);
        Assert.Equal("initial", result.Output["input"]);
        Assert.Equal("value1", result.Output["step1_result"]);
        Assert.Equal("value2", result.Output["step2_result"]);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyGraph_ReturnsSuccess()
    {
        var executor = new WorkflowExecutor((step, wfId, pk, ctx) =>
            Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>())));

        var graph = new WorkflowGraph(Guid.NewGuid().ToString(), "EmptyWorkflow", Array.Empty<WorkflowStep>());

        var result = await executor.ExecuteAsync(graph, new Dictionary<string, object>());

        Assert.True(result.Success);
    }
}
