namespace Whycespace.Tests.WssWorkflows;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Engines.T1M.WSS.Versioning;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;
using Xunit;

public sealed class WorkflowVersioningEngineTests
{
    private readonly WorkflowVersionStore _store;
    private readonly WorkflowVersioningEngine _engine;

    public WorkflowVersioningEngineTests()
    {
        _store = new WorkflowVersionStore();
        _engine = new WorkflowVersioningEngine(_store);
    }

    private static List<WorkflowStep> CreateSteps(params (string id, string name, string engine, string[] next)[] steps)
    {
        return steps.Select(s => new WorkflowStep(s.id, s.name, s.engine, s.next.ToList())).ToList();
    }

    private static WorkflowVersionCommand CreateCommand(
        string workflowName,
        string baseVersion,
        IReadOnlyList<WorkflowStep> newDefinition,
        string changeDescription = "test change",
        string requestedBy = "test-user")
    {
        return new WorkflowVersionCommand(
            Guid.NewGuid(),
            workflowName,
            baseVersion,
            newDefinition,
            changeDescription,
            requestedBy,
            DateTimeOffset.UtcNow);
    }

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(),
            "wf-test",
            "step-1",
            new PartitionKey("test", "default"),
            data);
    }

    // --- CreateVersion: minor version update (step additions) ---

    [Fact]
    public void CreateVersion_MinorUpdate_WhenStepsAdded_IncrementsMinor()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()));

        var newSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()),
            ("step-3", "Extra", "EngineC", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "1.0.0", newSteps);
        var result = _engine.CreateVersion(command, existing);

        Assert.True(result.Success);
        Assert.Equal("1.1.0", result.NewVersion);
        Assert.Equal("1.0.0", result.BaseVersion);
        Assert.Equal(CompatibilityLevel.BackwardCompatible, result.CompatibilityLevel);
    }

    // --- CreateVersion: patch update (no structural change) ---

    [Fact]
    public void CreateVersion_PatchUpdate_WhenNoStructuralChange_IncrementsPatch()
    {
        var steps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", steps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "1.0.0", steps, "metadata update");
        var result = _engine.CreateVersion(command, existing);

        Assert.True(result.Success);
        Assert.Equal("1.0.1", result.NewVersion);
        Assert.Equal(CompatibilityLevel.Compatible, result.CompatibilityLevel);
    }

    // --- CreateVersion: breaking change (step removed) ---

    [Fact]
    public void CreateVersion_BreakingChange_WhenStepRemoved_IncrementsMajor()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "Middle", "EngineB", new[] { "step-3" }),
            ("step-3", "End", "EngineC", Array.Empty<string>()));

        var newSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-3" }),
            ("step-3", "End", "EngineC", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "1.0.0", newSteps);
        var result = _engine.CreateVersion(command, existing);

        Assert.True(result.Success);
        Assert.Equal("2.0.0", result.NewVersion);
        Assert.Equal(CompatibilityLevel.Breaking, result.CompatibilityLevel);
    }

    // --- CreateVersion: breaking change (engine changed) ---

    [Fact]
    public void CreateVersion_BreakingChange_WhenEngineChanged_IncrementsMajor()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()));

        var newSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineX", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "1.0.0", newSteps);
        var result = _engine.CreateVersion(command, existing);

        Assert.True(result.Success);
        Assert.Equal("2.0.0", result.NewVersion);
        Assert.Equal(CompatibilityLevel.Breaking, result.CompatibilityLevel);
    }

    // --- CreateVersion: breaking change (transition removed) ---

    [Fact]
    public void CreateVersion_BreakingChange_WhenTransitionRemoved_IncrementsMajor()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2", "step-3" }),
            ("step-2", "Path A", "EngineB", Array.Empty<string>()),
            ("step-3", "Path B", "EngineC", Array.Empty<string>()));

        var newSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "Path A", "EngineB", Array.Empty<string>()),
            ("step-3", "Path B", "EngineC", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "1.0.0", newSteps);
        var result = _engine.CreateVersion(command, existing);

        Assert.True(result.Success);
        Assert.Equal("2.0.0", result.NewVersion);
        Assert.Equal(CompatibilityLevel.Breaking, result.CompatibilityLevel);
    }

    // --- Version comparison validation ---

    [Fact]
    public void CreateVersion_VersionComparison_MultipleIncrements()
    {
        var steps = CreateSteps(
            ("step-1", "Start", "EngineA", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "3.5.2", steps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "3.5.2", steps, "config update");
        var result = _engine.CreateVersion(command, existing);

        Assert.True(result.Success);
        Assert.Equal("3.5.3", result.NewVersion);
    }

    // --- BaseVersion not found ---

    [Fact]
    public void CreateVersion_BaseVersionNotFound_ReturnsFail()
    {
        var steps = CreateSteps(
            ("step-1", "Start", "EngineA", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", steps, DateTimeOffset.UtcNow)
        };

        var command = CreateCommand("wf-ride", "2.0.0", steps);
        var result = _engine.CreateVersion(command, existing);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    // --- First version (no existing versions) ---

    [Fact]
    public void CreateVersion_NoExistingVersions_CreatesFromBase()
    {
        var steps = CreateSteps(
            ("step-1", "Start", "EngineA", Array.Empty<string>()));

        var command = CreateCommand("new-workflow", "1.0.0", steps);
        var result = _engine.CreateVersion(command, Array.Empty<WfDefinition>());

        Assert.True(result.Success);
        Assert.Equal("1.0.1", result.NewVersion);
        Assert.Equal(CompatibilityLevel.Compatible, result.CompatibilityLevel);
    }

    // --- Deterministic version generation ---

    [Fact]
    public void CreateVersion_SameInput_ProducesSameOutput()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()));

        var newSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()),
            ("step-3", "New", "EngineC", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow)
        };

        var ts = DateTimeOffset.UtcNow;
        var cmd1 = new WorkflowVersionCommand(Guid.NewGuid(), "wf-ride", "1.0.0", newSteps, "test", "user", ts);
        var cmd2 = new WorkflowVersionCommand(Guid.NewGuid(), "wf-ride", "1.0.0", newSteps, "test", "user", ts);

        var result1 = _engine.CreateVersion(cmd1, existing);
        var result2 = _engine.CreateVersion(cmd2, existing);

        Assert.Equal(result1.NewVersion, result2.NewVersion);
        Assert.Equal(result1.CompatibilityLevel, result2.CompatibilityLevel);
    }

    // --- Concurrent version creation ---

    [Fact]
    public void CreateVersion_ConcurrentCalls_AllSucceed()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", Array.Empty<string>()));

        var existing = new WfDefinition[]
        {
            new("wf-ride", "Ride", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow)
        };

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var newSteps = CreateSteps(
                ("step-1", "Start", "EngineA", Array.Empty<string>()));
            var command = CreateCommand("wf-ride", "1.0.0", newSteps);
            return _engine.CreateVersion(command, existing);
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.All(tasks, t =>
        {
            Assert.True(t.Result.Success);
            Assert.Equal("1.0.1", t.Result.NewVersion);
        });
    }

    // --- IEngine ExecuteAsync: createVersion action ---

    [Fact]
    public async Task ExecuteAsync_CreateVersion_ReturnsSuccess()
    {
        var baseSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()));

        _store.Store(new WfDefinition(
            "test-wf", "TestWF", "Test", "1.0.0", baseSteps, DateTimeOffset.UtcNow));

        var newSteps = CreateSteps(
            ("step-1", "Start", "EngineA", new[] { "step-2" }),
            ("step-2", "End", "EngineB", Array.Empty<string>()),
            ("step-3", "New", "EngineC", Array.Empty<string>()));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "createVersion",
            ["workflowName"] = "test-wf",
            ["baseVersion"] = "1.0.0",
            ["newDefinition"] = (IReadOnlyList<WorkflowStep>)newSteps,
            ["changeDescription"] = "Added new step",
            ["requestedBy"] = "test-user"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("1.1.0", result.Output["newVersion"]);
        Assert.Equal("BackwardCompatible", result.Output["compatibilityLevel"]);
        Assert.Single(result.Events);
        Assert.Equal("WorkflowVersionCreated", result.Events[0].EventType);
    }

    // --- IEngine ExecuteAsync: missing workflowName ---

    [Fact]
    public async Task ExecuteAsync_CreateVersion_MissingWorkflowName_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "createVersion",
            ["baseVersion"] = "1.0.0"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("workflowName", result.Output["error"] as string);
    }

    // --- IEngine ExecuteAsync: invalid base version format ---

    [Fact]
    public async Task ExecuteAsync_CreateVersion_InvalidBaseVersion_Fails()
    {
        var steps = CreateSteps(("step-1", "S", "E", Array.Empty<string>()));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "createVersion",
            ["workflowName"] = "test-wf",
            ["baseVersion"] = "v1",
            ["newDefinition"] = (IReadOnlyList<WorkflowStep>)steps
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid base version", result.Output["error"] as string);
    }

    // --- IEngine ExecuteAsync: unknown action ---

    [Fact]
    public async Task ExecuteAsync_UnknownAction_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "invalidAction"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Unknown action", result.Output["error"] as string);
    }

    // --- IEngine ExecuteAsync: listVersions action ---

    [Fact]
    public async Task ExecuteAsync_ListVersions_ReturnsVersionList()
    {
        var steps = CreateSteps(("step-1", "S", "E", Array.Empty<string>()));

        _store.Store(new WfDefinition(
            "wf-list", "ListWF", "Test", "1.0.0", steps, DateTimeOffset.UtcNow));
        _store.Store(new WfDefinition(
            "wf-list", "ListWF", "Test", "1.1.0", steps, DateTimeOffset.UtcNow));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "listVersions",
            ["workflowId"] = "wf-list"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Output["count"]);
    }

    // --- IEngine ExecuteAsync: getVersion action ---

    [Fact]
    public async Task ExecuteAsync_GetVersion_ReturnsSpecificVersion()
    {
        var steps = CreateSteps(("step-1", "S", "E", Array.Empty<string>()));

        _store.Store(new WfDefinition(
            "wf-get", "GetWF", "Test", "1.0.0", steps, DateTimeOffset.UtcNow));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "getVersion",
            ["workflowId"] = "wf-get",
            ["version"] = "1.0.0"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("1.0.0", result.Output["version"]);
    }

    // --- IEngine ExecuteAsync: getLatest action ---

    [Fact]
    public async Task ExecuteAsync_GetLatest_ReturnsLatestVersion()
    {
        var steps = CreateSteps(("step-1", "S", "E", Array.Empty<string>()));

        _store.Store(new WfDefinition(
            "wf-latest", "LatestWF", "Test", "1.0.0", steps, DateTimeOffset.UtcNow));
        _store.Store(new WfDefinition(
            "wf-latest", "LatestWF", "Test", "2.0.0", steps, DateTimeOffset.UtcNow));

        var context = CreateContext(new Dictionary<string, object>
        {
            ["action"] = "getLatest",
            ["workflowId"] = "wf-latest"
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("2.0.0", result.Output["version"]);
    }

    // --- Engine Name ---

    [Fact]
    public void Name_ReturnsWorkflowVersioning()
    {
        Assert.Equal("WorkflowVersioning", _engine.Name);
    }

    // --- DetermineCompatibility unit tests ---

    [Fact]
    public void DetermineCompatibility_NullBase_ReturnsCompatible()
    {
        var newSteps = CreateSteps(("step-1", "S", "E", Array.Empty<string>()));

        var result = WorkflowVersioningEngine.DetermineCompatibility(null, newSteps);

        Assert.Equal(CompatibilityLevel.Compatible, result);
    }

    [Fact]
    public void DetermineCompatibility_EmptyBase_ReturnsCompatible()
    {
        var newSteps = CreateSteps(("step-1", "S", "E", Array.Empty<string>()));

        var result = WorkflowVersioningEngine.DetermineCompatibility(
            Array.Empty<WorkflowStep>(), newSteps);

        Assert.Equal(CompatibilityLevel.Compatible, result);
    }

    // --- IncrementVersion unit tests ---

    [Theory]
    [InlineData("1.0.0", CompatibilityLevel.Breaking, "2.0.0")]
    [InlineData("1.0.0", CompatibilityLevel.BackwardCompatible, "1.1.0")]
    [InlineData("1.0.0", CompatibilityLevel.Compatible, "1.0.1")]
    [InlineData("3.5.2", CompatibilityLevel.Breaking, "4.0.0")]
    [InlineData("3.5.2", CompatibilityLevel.BackwardCompatible, "3.6.0")]
    [InlineData("3.5.2", CompatibilityLevel.Compatible, "3.5.3")]
    public void IncrementVersion_CorrectlyIncrements(
        string baseVersion, CompatibilityLevel level, string expected)
    {
        var result = WorkflowVersioningEngine.IncrementVersion(baseVersion, level);

        Assert.Equal(expected, result);
    }
}
