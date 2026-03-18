namespace Whycespace.Tests.WssWorkflows;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T1M.WSS.Step;
using Xunit;

public sealed class WorkflowStepEngineMappingEngineTests
{
    private readonly WorkflowStepEngineMappingEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(),
            "wf-test",
            "step-1",
            new PartitionKey("test-partition"),
            data);
    }

    private static List<StepEngineMappingInput> CreateSteps(
        params (string id, string name, string engine)[] steps)
    {
        return steps.Select(s => new StepEngineMappingInput(s.id, s.name, s.engine)).ToList();
    }

    // --- Valid mapping ---

    [Fact]
    public async Task ExecuteAsync_ValidMapping_ReturnsSuccess()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"),
            ("step-2", "VerifyIdentity", "IdentityVerificationEngine"));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-001",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkflowStepEngineMappingResolved", result.Events[0].EventType);
        Assert.Equal("wf-test-001", result.Output["workflowId"]);
        Assert.Equal(2, result.Output["stepCount"]);
    }

    [Fact]
    public async Task ExecuteAsync_ValidMapping_ContainsMappingDetails()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"),
            ("step-2", "VerifyIdentity", "IdentityVerificationEngine"));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-002",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var mappings = result.Output["mappings"] as List<Dictionary<string, object>>;
        Assert.NotNull(mappings);
        Assert.Equal(2, mappings!.Count);
        Assert.Equal("step-1", mappings[0]["stepId"]);
        Assert.Equal("VaultCreationEngine", mappings[0]["engineName"]);
        Assert.Equal("step-2", mappings[1]["stepId"]);
        Assert.Equal("IdentityVerificationEngine", mappings[1]["engineName"]);
    }

    // --- Missing engine name ---

    [Fact]
    public async Task ExecuteAsync_MissingEngineName_ReturnsFail()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"),
            ("step-2", "VerifyIdentity", ""));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-003",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("EngineName must not be empty", result.Output["error"] as string);
    }

    // --- Duplicate step ID ---

    [Fact]
    public async Task ExecuteAsync_DuplicateStepId_ReturnsFail()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"),
            ("step-1", "VerifyIdentity", "IdentityVerificationEngine"));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-004",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Duplicate step ID", result.Output["error"] as string);
    }

    // --- Empty workflow ID ---

    [Fact]
    public async Task ExecuteAsync_EmptyWorkflowId_ReturnsFail()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("WorkflowId must not be empty", result.Output["error"] as string);
    }

    // --- No steps ---

    [Fact]
    public async Task ExecuteAsync_NoSteps_ReturnsFail()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-005",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)Array.Empty<StepEngineMappingInput>()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("at least one step", result.Output["error"] as string);
    }

    // --- Missing step name ---

    [Fact]
    public async Task ExecuteAsync_MissingStepName_ReturnsFail()
    {
        var steps = CreateSteps(
            ("step-1", "", "VaultCreationEngine"));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-006",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("StepName must not be empty", result.Output["error"] as string);
    }

    // --- Missing step ID ---

    [Fact]
    public async Task ExecuteAsync_MissingStepId_ReturnsFail()
    {
        var steps = CreateSteps(
            ("", "CreateVault", "VaultCreationEngine"));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["workflowId"] = "wf-test-007",
            ["workflowSteps"] = (IReadOnlyList<StepEngineMappingInput>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("non-empty StepId", result.Output["error"] as string);
    }

    // --- Deterministic mapping ---

    [Fact]
    public void ResolveMapping_SameInput_ProducesSameMappings()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"),
            ("step-2", "VerifyIdentity", "IdentityVerificationEngine"));

        var command = new WorkflowStepEngineMappingCommand("wf-deterministic", steps);

        var result1 = _engine.ResolveMapping(command);
        var result2 = _engine.ResolveMapping(command);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.StepEngineMappings.Count, result2.StepEngineMappings.Count);

        for (var i = 0; i < result1.StepEngineMappings.Count; i++)
        {
            Assert.Equal(result1.StepEngineMappings[i].StepId, result2.StepEngineMappings[i].StepId);
            Assert.Equal(result1.StepEngineMappings[i].EngineName, result2.StepEngineMappings[i].EngineName);
        }
    }

    // --- Concurrent mapping ---

    [Fact]
    public void ResolveMapping_ConcurrentCalls_AllSucceed()
    {
        var steps = CreateSteps(
            ("step-1", "CreateVault", "VaultCreationEngine"),
            ("step-2", "VerifyIdentity", "IdentityVerificationEngine"));

        var command = new WorkflowStepEngineMappingCommand("wf-concurrent", steps);

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            _engine.ResolveMapping(command))).ToArray();

        Task.WaitAll(tasks);

        Assert.All(tasks, t =>
        {
            Assert.True(t.Result.Success);
            Assert.Equal(2, t.Result.StepEngineMappings.Count);
            Assert.Equal("wf-concurrent", t.Result.WorkflowId);
        });
    }

    // --- Unknown action ---

    [Fact]
    public async Task ExecuteAsync_UnknownAction_ReturnsFail()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "invalidAction"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unknown action", result.Output["error"] as string);
    }

    // --- Engine name ---

    [Fact]
    public void Name_ReturnsWorkflowStepEngineMapping()
    {
        Assert.Equal("WorkflowStepEngineMapping", _engine.Name);
    }

    // --- ResolveMapping typed result ---

    [Fact]
    public void ResolveMapping_ValidInput_ReturnsMappingsWithCorrectStructure()
    {
        var steps = CreateSteps(
            ("create-vault", "CreateVault", "VaultCreationEngine"),
            ("verify-id", "VerifyIdentity", "IdentityVerificationEngine"),
            ("approve", "Approve", "ApprovalEngine"));

        var command = new WorkflowStepEngineMappingCommand("wf-structure", steps);
        var result = _engine.ResolveMapping(command);

        Assert.True(result.Success);
        Assert.Equal("wf-structure", result.WorkflowId);
        Assert.Equal(3, result.StepEngineMappings.Count);
        Assert.Null(result.Error);

        Assert.Equal("create-vault", result.StepEngineMappings[0].StepId);
        Assert.Equal("VaultCreationEngine", result.StepEngineMappings[0].EngineName);
        Assert.Equal("verify-id", result.StepEngineMappings[1].StepId);
        Assert.Equal("IdentityVerificationEngine", result.StepEngineMappings[1].EngineName);
        Assert.Equal("approve", result.StepEngineMappings[2].StepId);
        Assert.Equal("ApprovalEngine", result.StepEngineMappings[2].EngineName);
    }
}
