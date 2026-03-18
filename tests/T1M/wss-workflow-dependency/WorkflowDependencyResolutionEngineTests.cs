using Whycespace.Engines.T1M.WSS.Resolution;

namespace Whycespace.WSS.WorkflowDependency.Tests;

public sealed class WorkflowDependencyResolutionEngineTests
{
    // --- Helpers ---

    private static DependencyStep Step(string id, string name, string engine, params string[] deps)
        => new(id, name, engine, deps.ToList());

    private static WorkflowDependencyCommand Command(
        string workflowId,
        IReadOnlyList<DependencyStep> steps,
        params string[] completed)
        => new(workflowId, steps, completed.ToList());

    // --- Test 1: Step without dependencies executes immediately ---

    [Fact]
    public void ResolveDependencies_StepWithNoDependencies_IsReady()
    {
        var steps = new[]
        {
            Step("step-1", "Init", "InitEngine"),
            Step("step-2", "Process", "ProcessEngine", "step-1")
        };
        var command = Command("wf-1", steps);

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Contains("step-1", result.ReadySteps);
        Assert.DoesNotContain("step-2", result.ReadySteps);
    }

    // --- Test 2: Step waits for dependency completion ---

    [Fact]
    public void ResolveDependencies_StepWithUnmetDependency_IsBlocked()
    {
        var steps = new[]
        {
            Step("step-1", "Init", "InitEngine"),
            Step("step-2", "Process", "ProcessEngine", "step-1"),
            Step("step-3", "Finalize", "FinalEngine", "step-2")
        };
        var command = Command("wf-1", steps);

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Contains("step-2", result.BlockedSteps);
        Assert.Contains("step-3", result.BlockedSteps);
    }

    [Fact]
    public void ResolveDependencies_StepWithMetDependency_IsReady()
    {
        var steps = new[]
        {
            Step("step-1", "Init", "InitEngine"),
            Step("step-2", "Process", "ProcessEngine", "step-1"),
            Step("step-3", "Finalize", "FinalEngine", "step-2")
        };
        var command = Command("wf-1", steps, "step-1");

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Contains("step-2", result.ReadySteps);
        Assert.Contains("step-3", result.BlockedSteps);
    }

    // --- Test 3: Multi-step dependency chain resolution ---

    [Fact]
    public void ResolveDependencies_MultiStepChain_ResolvesCorrectly()
    {
        var steps = new[]
        {
            Step("a", "A", "EngineA"),
            Step("b", "B", "EngineB", "a"),
            Step("c", "C", "EngineC", "b"),
            Step("d", "D", "EngineD", "c")
        };
        var command = Command("wf-chain", steps, "a", "b");

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Equal(new[] { "c" }, result.ReadySteps);
        Assert.Equal(new[] { "d" }, result.BlockedSteps);
        Assert.Contains("a", result.CompletedSteps);
        Assert.Contains("b", result.CompletedSteps);
    }

    // --- Test 4: Parallel step detection ---

    [Fact]
    public void ResolveDependencies_ParallelSteps_AllReadySimultaneously()
    {
        var steps = new[]
        {
            Step("start", "Start", "InitEngine"),
            Step("branch-a", "BranchA", "EngineA", "start"),
            Step("branch-b", "BranchB", "EngineB", "start"),
            Step("branch-c", "BranchC", "EngineC", "start"),
            Step("merge", "Merge", "MergeEngine", "branch-a", "branch-b", "branch-c")
        };
        var command = Command("wf-parallel", steps, "start");

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Equal(3, result.ReadySteps.Count);
        Assert.Contains("branch-a", result.ReadySteps);
        Assert.Contains("branch-b", result.ReadySteps);
        Assert.Contains("branch-c", result.ReadySteps);
        Assert.Contains("merge", result.BlockedSteps);
    }

    [Fact]
    public void ResolveDependencies_ParallelSteps_MergeUnblocksWhenAllComplete()
    {
        var steps = new[]
        {
            Step("start", "Start", "InitEngine"),
            Step("branch-a", "BranchA", "EngineA", "start"),
            Step("branch-b", "BranchB", "EngineB", "start"),
            Step("merge", "Merge", "MergeEngine", "branch-a", "branch-b")
        };
        var command = Command("wf-parallel", steps, "start", "branch-a", "branch-b");

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Single(result.ReadySteps);
        Assert.Contains("merge", result.ReadySteps);
        Assert.Empty(result.BlockedSteps);
    }

    // --- Test 5: Large workflow dependency evaluation ---

    [Fact]
    public void ResolveDependencies_LargeWorkflow_HandlesCorrectly()
    {
        var steps = new List<DependencyStep>();

        // Create a chain of 100 steps
        steps.Add(Step("step-0", "Step0", "Engine0"));
        for (int i = 1; i < 100; i++)
        {
            steps.Add(Step($"step-{i}", $"Step{i}", $"Engine{i}", $"step-{i - 1}"));
        }

        // Complete first 50 steps
        var completed = Enumerable.Range(0, 50).Select(i => $"step-{i}").ToArray();
        var command = Command("wf-large", steps, completed);

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        // step-50 should be ready (its dependency step-49 is completed)
        Assert.Contains("step-50", result.ReadySteps);
        Assert.Single(result.ReadySteps);

        // steps 51-99 should be blocked
        Assert.Equal(49, result.BlockedSteps.Count);
        Assert.Equal(50, result.CompletedSteps.Count);
    }

    // --- Test 6: Concurrent dependency resolution ---

    [Fact]
    public async Task ResolveDependencies_ConcurrentCalls_IsDeterministic()
    {
        var steps = new[]
        {
            Step("a", "A", "EngineA"),
            Step("b", "B", "EngineB"),
            Step("c", "C", "EngineC", "a"),
            Step("d", "D", "EngineD", "b"),
            Step("e", "E", "EngineE", "c", "d")
        };
        var command = Command("wf-concurrent", steps, "a");

        var results = new WorkflowDependencyResolutionResult[50];
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() =>
            {
                results[i] = WorkflowDependencyResolutionEngine.ResolveDependencies(command);
            })).ToArray();

        await Task.WhenAll(tasks);

        // All results must be identical
        var baseline = results[0];
        foreach (var result in results)
        {
            Assert.Equal(baseline.WorkflowId, result.WorkflowId);
            Assert.Equal(baseline.ReadySteps, result.ReadySteps);
            Assert.Equal(baseline.BlockedSteps, result.BlockedSteps);
            Assert.Equal(baseline.CompletedSteps, result.CompletedSteps);
        }
    }

    // --- Additional: Completed steps are excluded from ready/blocked ---

    [Fact]
    public void ResolveDependencies_CompletedSteps_ExcludedFromReadyAndBlocked()
    {
        var steps = new[]
        {
            Step("step-1", "Init", "InitEngine"),
            Step("step-2", "Process", "ProcessEngine", "step-1")
        };
        var command = Command("wf-1", steps, "step-1", "step-2");

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Empty(result.ReadySteps);
        Assert.Empty(result.BlockedSteps);
        Assert.Equal(2, result.CompletedSteps.Count);
    }

    // --- Result metadata ---

    [Fact]
    public void ResolveDependencies_ResultContainsWorkflowId()
    {
        var steps = new[] { Step("s1", "S1", "E1") };
        var command = Command("my-workflow-42", steps);

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Equal("my-workflow-42", result.WorkflowId);
    }

    [Fact]
    public void ResolveDependencies_ResultContainsEvaluatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var steps = new[] { Step("s1", "S1", "E1") };
        var command = Command("wf-1", steps);

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.True(result.EvaluatedAt >= before);
        Assert.True(result.EvaluatedAt <= DateTimeOffset.UtcNow);
    }

    // --- Deterministic ordering ---

    [Fact]
    public void ResolveDependencies_ReadySteps_AreSortedDeterministically()
    {
        var steps = new[]
        {
            Step("zebra", "Zebra", "E1"),
            Step("alpha", "Alpha", "E2"),
            Step("middle", "Middle", "E3")
        };
        var command = Command("wf-sort", steps);

        var result = WorkflowDependencyResolutionEngine.ResolveDependencies(command);

        Assert.Equal(new[] { "alpha", "middle", "zebra" }, result.ReadySteps);
    }

    // --- Diamond dependency pattern ---

    [Fact]
    public void ResolveDependencies_DiamondPattern_ResolvesCorrectly()
    {
        // Diamond: A -> B, A -> C, B -> D, C -> D
        var steps = new[]
        {
            Step("a", "A", "EngineA"),
            Step("b", "B", "EngineB", "a"),
            Step("c", "C", "EngineC", "a"),
            Step("d", "D", "EngineD", "b", "c")
        };

        // After completing A, both B and C should be ready
        var result1 = WorkflowDependencyResolutionEngine.ResolveDependencies(
            Command("wf-diamond", steps, "a"));
        Assert.Equal(new[] { "b", "c" }, result1.ReadySteps);
        Assert.Equal(new[] { "d" }, result1.BlockedSteps);

        // After completing A and B, only C is ready (D still blocked on C)
        var result2 = WorkflowDependencyResolutionEngine.ResolveDependencies(
            Command("wf-diamond", steps, "a", "b"));
        Assert.Equal(new[] { "c" }, result2.ReadySteps);
        Assert.Equal(new[] { "d" }, result2.BlockedSteps);

        // After completing A, B, and C, D is ready
        var result3 = WorkflowDependencyResolutionEngine.ResolveDependencies(
            Command("wf-diamond", steps, "a", "b", "c"));
        Assert.Equal(new[] { "d" }, result3.ReadySteps);
        Assert.Empty(result3.BlockedSteps);
    }
}
