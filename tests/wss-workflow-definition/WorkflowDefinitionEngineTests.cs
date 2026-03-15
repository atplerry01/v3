using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T1M.WSS.Definition;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public sealed class WorkflowDefinitionEngineTests
{
    private readonly WorkflowDefinitionEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "DefineWorkflow",
            new PartitionKey("partition-1"),
            data);
    }

    private static Dictionary<string, object> ValidWorkflowData() => new()
    {
        ["workflowName"] = "Taxi Ride Request",
        ["workflowDescription"] = "End-to-end taxi ride flow",
        ["workflowVersion"] = "1.0.0",
        ["requestedBy"] = "system-test",
        ["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "Request Ride", "RideEngine", new List<string>(), TimeSpan.FromMinutes(5), null),
            new("step-2", "Match Driver", "DriverMatchEngine", new List<string> { "step-1" }, TimeSpan.FromMinutes(3), null),
            new("step-3", "Complete Payment", "PaymentEngine", new List<string> { "step-2" }, TimeSpan.FromMinutes(5),
                new WorkflowRetryPolicyInput(3, TimeSpan.FromSeconds(10)))
        },
        ["workflowParameters"] = new List<WorkflowParameterInput>
        {
            new("pickupLocation", "string", true),
            new("destinationLocation", "string", true),
            new("rideType", "string", false)
        }
    };

    // ─── Test 1: Valid workflow definition ─────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ValidWorkflow_ShouldSucceed()
    {
        var context = CreateContext(ValidWorkflowData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkflowDefinitionCreated", result.Events[0].EventType);
        Assert.Equal("Taxi Ride Request", result.Output["workflowName"]);
        Assert.Equal("1.0.0", result.Output["workflowVersion"]);
        Assert.Equal(3, result.Output["stepCount"]);
        Assert.Equal(3, result.Output["parameterCount"]);
        Assert.True(result.Output.ContainsKey("workflowId"));
    }

    // ─── Test 2: Deterministic workflow ID generation ──────────────────────

    [Fact]
    public async Task ExecuteAsync_SameInput_ShouldProduceSameWorkflowId()
    {
        var data1 = ValidWorkflowData();
        var data2 = ValidWorkflowData();

        var result1 = await _engine.ExecuteAsync(CreateContext(data1));
        var result2 = await _engine.ExecuteAsync(CreateContext(data2));

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Output["workflowId"], result2.Output["workflowId"]);
    }

    [Fact]
    public void GenerateDeterministicWorkflowId_SameInput_ShouldProduceSameHash()
    {
        var steps = new List<WorkflowStepInput>
        {
            new("step-1", "Step One", "EngineA", new List<string>(), TimeSpan.FromMinutes(5), null),
            new("step-2", "Step Two", "EngineB", new List<string> { "step-1" }, TimeSpan.FromMinutes(3), null)
        };

        var id1 = WorkflowDefinitionEngine.GenerateDeterministicWorkflowId("TestFlow", "1.0.0", steps);
        var id2 = WorkflowDefinitionEngine.GenerateDeterministicWorkflowId("TestFlow", "1.0.0", steps);

        Assert.Equal(id1, id2);
        Assert.Equal(64, id1.Length); // SHA256 hex string
    }

    [Fact]
    public void GenerateDeterministicWorkflowId_DifferentVersion_ShouldProduceDifferentHash()
    {
        var steps = new List<WorkflowStepInput>
        {
            new("step-1", "Step One", "EngineA", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var id1 = WorkflowDefinitionEngine.GenerateDeterministicWorkflowId("TestFlow", "1.0.0", steps);
        var id2 = WorkflowDefinitionEngine.GenerateDeterministicWorkflowId("TestFlow", "2.0.0", steps);

        Assert.NotEqual(id1, id2);
    }

    // ─── Test 3: Dependency validation ─────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_InvalidDependencyReference_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-99" }, TimeSpan.FromMinutes(5), null),
            new("step-2", "End", "EngineB", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("step-99", result.Output["error"] as string);
        Assert.Contains("does not exist", result.Output["error"] as string);
    }

    [Fact]
    public async Task ExecuteAsync_CircularDependency_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-3" }, TimeSpan.FromMinutes(5), null),
            new("step-2", "Second", "EngineB", new List<string> { "step-1" }, TimeSpan.FromMinutes(5), null),
            new("step-3", "Third", "EngineC", new List<string> { "step-2" }, TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("Circular dependency", result.Output["error"] as string);
    }

    // ─── Test 4: Step validation ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptySteps_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>();

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("at least one step", result.Output["error"] as string);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateStepIds_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "First", "EngineA", new List<string>(), TimeSpan.FromMinutes(5), null),
            new("step-1", "Duplicate", "EngineB", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("Duplicate step ID", result.Output["error"] as string);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyStepId_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("", "Missing ID", "EngineA", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("StepId", result.Output["error"] as string);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyStepName_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "", "EngineA", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("StepName", result.Output["error"] as string);
    }

    // ─── Test 5: Engine mapping validation ─────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_MissingEngineName_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "No Engine", "", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("EngineName", result.Output["error"] as string);
    }

    // ─── Test 6: Workflow name/version validation ──────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptyWorkflowName_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowName"] = "";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("WorkflowName", result.Output["error"] as string);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyWorkflowVersion_ShouldFail()
    {
        var data = ValidWorkflowData();
        data["workflowVersion"] = "";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("WorkflowVersion", result.Output["error"] as string);
    }

    // ─── Test 7: Concurrent command execution ──────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecution_ShouldBeThreadSafe()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
        {
            var data = ValidWorkflowData();
            data["workflowName"] = $"Workflow-{i}";
            return _engine.ExecuteAsync(CreateContext(data));
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));

        var workflowIds = results.Select(r => r.Output["workflowId"]).ToHashSet();
        Assert.Equal(50, workflowIds.Count); // All unique IDs
    }

    // ─── Test 8: Event structure ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ValidWorkflow_ShouldProduceCorrectEvent()
    {
        var context = CreateContext(ValidWorkflowData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var evt = result.Events[0];
        Assert.Equal("WorkflowDefinitionCreated", evt.EventType);
        Assert.Equal("Taxi Ride Request", evt.Payload["workflowName"]);
        Assert.Equal("1.0.0", evt.Payload["workflowVersion"]);
        Assert.Equal(3, evt.Payload["stepCount"]);
        Assert.Equal(3, evt.Payload["parameterCount"]);
        Assert.Equal("system-test", evt.Payload["requestedBy"]);
        Assert.Equal(1, evt.Payload["eventVersion"]);
        Assert.Equal("whyce.wss.workflow.events", evt.Payload["topic"]);
    }

    // ─── Test 9: No parameters workflow ────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NoParameters_ShouldSucceed()
    {
        var data = ValidWorkflowData();
        data["workflowParameters"] = new List<WorkflowParameterInput>();

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(0, result.Output["parameterCount"]);
    }

    // ─── Test 10: Single step workflow ─────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SingleStep_ShouldSucceed()
    {
        var data = ValidWorkflowData();
        data["workflowSteps"] = new List<WorkflowStepInput>
        {
            new("step-1", "Only Step", "SimpleEngine", new List<string>(), TimeSpan.FromMinutes(5), null)
        };

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(1, result.Output["stepCount"]);
    }
}
