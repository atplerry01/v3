using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.System.Midstream.WSS.Models;
using WfGraph = Whycespace.System.Midstream.WSS.Models.WorkflowGraph;

namespace Whycespace.WSS.WorkflowGraph.Tests;

public class WorkflowGraphEngineTests
{
    private readonly WorkflowGraphEngine _engine;

    public WorkflowGraphEngineTests()
    {
        _engine = new WorkflowGraphEngine();
    }

    private static List<WorkflowStepDefinition> LinearSteps() => new()
    {
        new("ValidatePassenger", "Validate Passenger", "ValidationEngine", "Validates passenger", new List<string> { "FindDriver" }, null),
        new("FindDriver", "Find Driver", "MatchEngine", "Finds available driver", new List<string> { "CreateRide" }, null),
        new("CreateRide", "Create Ride", "RideEngine", "Creates the ride", new List<string>(), null)
    };

    private static List<WorkflowStepDefinition> BranchingSteps() => new()
    {
        new("Start", "Start", "StartEngine", "Entry point", new List<string> { "BranchA", "BranchB" }, null),
        new("BranchA", "Branch A", "EngineA", "First branch", new List<string> { "End" }, null),
        new("BranchB", "Branch B", "EngineB", "Second branch", new List<string> { "End" }, null),
        new("End", "End", "EndEngine", "Terminal step", new List<string>(), null)
    };

    // 1. Valid linear workflow
    [Fact]
    public void BuildGraph_LinearWorkflow_ShouldBuildCorrectTransitions()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        Assert.Equal(3, graph.Transitions.Count);
        Assert.Equal(new[] { "FindDriver" }, graph.Transitions["ValidatePassenger"]);
        Assert.Equal(new[] { "CreateRide" }, graph.Transitions["FindDriver"]);
        Assert.Empty(graph.Transitions["CreateRide"]);
    }

    [Fact]
    public void ValidateGraph_LinearWorkflow_ShouldReturnNoViolations()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        var violations = _engine.ValidateGraph(graph);

        Assert.Empty(violations);
    }

    // 2. Valid branching workflow
    [Fact]
    public void ValidateGraph_BranchingWorkflow_ShouldReturnNoViolations()
    {
        var graph = _engine.BuildGraph(BranchingSteps());

        var violations = _engine.ValidateGraph(graph);

        Assert.Empty(violations);
    }

    // 3. Detect circular dependency
    [Fact]
    public void ValidateGraph_CircularDependency_ShouldDetect()
    {
        var steps = new List<WorkflowStepDefinition>
        {
            new("A", "Step A", "EngineA", "First", new List<string> { "B" }, null),
            new("B", "Step B", "EngineB", "Second", new List<string> { "C" }, null),
            new("C", "Step C", "EngineC", "Third", new List<string> { "A" }, null)
        };
        var graph = _engine.BuildGraph(steps);

        var violations = _engine.ValidateGraph(graph);

        Assert.Contains(violations, v => v.Contains("Circular dependency"));
    }

    // 4. Detect missing node
    [Fact]
    public void ValidateGraph_MissingNode_ShouldDetect()
    {
        var graph = new WfGraph("wf-1", new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = new List<string> { "B" },
            ["B"] = new List<string> { "NonExistent" }
        });

        var violations = _engine.ValidateGraph(graph);

        Assert.Contains(violations, v => v.Contains("undefined node 'NonExistent'"));
    }

    // 5. Detect orphan node
    [Fact]
    public void ValidateGraph_OrphanNode_ShouldDetect()
    {
        var graph = new WfGraph("wf-1", new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = new List<string> { "B" },
            ["B"] = new List<string>(),
            ["Orphan"] = new List<string>()
        });

        var violations = _engine.ValidateGraph(graph);

        Assert.Contains(violations, v => v.Contains("Orphan node detected: 'Orphan'"));
    }

    // 6. Detect missing start node
    [Fact]
    public void ValidateGraph_NoStartNode_ShouldDetect()
    {
        var graph = new WfGraph("wf-1", new Dictionary<string, IReadOnlyList<string>>
        {
            ["A"] = new List<string> { "B" },
            ["B"] = new List<string> { "A" }
        });

        var violations = _engine.ValidateGraph(graph);

        Assert.Contains(violations, v => v.Contains("no start step"));
    }

    // 7. Next step resolution
    [Fact]
    public void GetNextSteps_ShouldReturnCorrectSteps()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        var next = _engine.GetNextSteps(graph, "FindDriver");

        Assert.Single(next);
        Assert.Equal("CreateRide", next[0]);
    }

    [Fact]
    public void GetNextSteps_TerminalStep_ShouldReturnEmpty()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        var next = _engine.GetNextSteps(graph, "CreateRide");

        Assert.Empty(next);
    }

    [Fact]
    public void GetNextSteps_UnknownStep_ShouldThrow()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        Assert.Throws<KeyNotFoundException>(() => _engine.GetNextSteps(graph, "NonExistent"));
    }

    // 8. Start step detection
    [Fact]
    public void GetStartSteps_LinearWorkflow_ShouldReturnSingleStart()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        var starts = _engine.GetStartSteps(graph);

        Assert.Single(starts);
        Assert.Equal("ValidatePassenger", starts[0]);
    }

    [Fact]
    public void GetStartSteps_BranchingWorkflow_ShouldReturnSingleStart()
    {
        var graph = _engine.BuildGraph(BranchingSteps());

        var starts = _engine.GetStartSteps(graph);

        Assert.Single(starts);
        Assert.Equal("Start", starts[0]);
    }

    // 9. Execution Order (Topological Sort)
    [Fact]
    public void ComputeExecutionOrder_LinearWorkflow_ShouldReturnCorrectOrder()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        var order = _engine.ComputeExecutionOrder(graph);

        Assert.Equal(3, order.Count);
        Assert.True(order.ToList().IndexOf("ValidatePassenger") < order.ToList().IndexOf("FindDriver"));
        Assert.True(order.ToList().IndexOf("FindDriver") < order.ToList().IndexOf("CreateRide"));
    }

    [Fact]
    public void ComputeExecutionOrder_BranchingWorkflow_ShouldRespectDependencies()
    {
        var graph = _engine.BuildGraph(BranchingSteps());

        var order = _engine.ComputeExecutionOrder(graph);

        Assert.Equal(4, order.Count);
        Assert.Equal("Start", order[0]);
        Assert.True(order.ToList().IndexOf("BranchA") < order.ToList().IndexOf("End"));
        Assert.True(order.ToList().IndexOf("BranchB") < order.ToList().IndexOf("End"));
    }

    // 10. Parallel Groups
    [Fact]
    public void ComputeParallelGroups_BranchingWorkflow_ShouldIdentifyParallelSteps()
    {
        var graph = _engine.BuildGraph(BranchingSteps());

        var groups = _engine.ComputeParallelGroups(graph);

        Assert.Equal(3, groups.Count);
        Assert.Single(groups[0]);
        Assert.Equal(2, groups[1].Count);
        Assert.Single(groups[2]);
    }

    [Fact]
    public void ComputeParallelGroups_LinearWorkflow_ShouldReturnSingleStepGroups()
    {
        var graph = _engine.BuildGraph(LinearSteps());

        var groups = _engine.ComputeParallelGroups(graph);

        Assert.Equal(3, groups.Count);
        foreach (var group in groups)
            Assert.Single(group);
    }

    // 11. BuildExecutionGraph (Full DAG Construction)
    [Fact]
    public void BuildExecutionGraph_ValidDAG_ShouldReturnSuccessResult()
    {
        var command = new WorkflowGraphCommand("wf-1", "TestWorkflow", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("A", "Step A", "EngineA", new List<string>()),
            new("B", "Step B", "EngineB", new List<string> { "A" }),
            new("C", "Step C", "EngineC", new List<string> { "A" }),
            new("D", "Step D", "EngineD", new List<string> { "B", "C" })
        });

        var result = _engine.BuildExecutionGraph(command);

        Assert.True(result.Success);
        Assert.NotNull(result.Graph);
        Assert.Equal("wf-1", result.Graph!.WorkflowId);
        Assert.Equal(4, result.Graph.Nodes.Count);
        Assert.Equal(4, result.Graph.Edges.Count);
        Assert.Equal(4, result.Graph.ExecutionOrder.Count);
        Assert.True(result.Graph.ExecutionOrder.ToList().IndexOf("A") < result.Graph.ExecutionOrder.ToList().IndexOf("B"));
        Assert.True(result.Graph.ExecutionOrder.ToList().IndexOf("A") < result.Graph.ExecutionOrder.ToList().IndexOf("C"));
        Assert.True(result.Graph.ExecutionOrder.ToList().IndexOf("B") < result.Graph.ExecutionOrder.ToList().IndexOf("D"));
    }

    [Fact]
    public void BuildExecutionGraph_CircularDependency_ShouldReturnFailure()
    {
        var command = new WorkflowGraphCommand("wf-1", "CyclicWorkflow", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("A", "Step A", "EngineA", new List<string> { "C" }),
            new("B", "Step B", "EngineB", new List<string> { "A" }),
            new("C", "Step C", "EngineC", new List<string> { "B" })
        });

        var result = _engine.BuildExecutionGraph(command);

        Assert.False(result.Success);
        Assert.Contains("Circular dependency", result.ErrorMessage);
    }

    [Fact]
    public void BuildExecutionGraph_MissingDependency_ShouldReturnFailure()
    {
        var command = new WorkflowGraphCommand("wf-1", "MissingDep", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("A", "Step A", "EngineA", new List<string>()),
            new("B", "Step B", "EngineB", new List<string> { "NonExistent" })
        });

        var result = _engine.BuildExecutionGraph(command);

        Assert.False(result.Success);
        Assert.Contains("undefined dependency 'NonExistent'", result.ErrorMessage);
    }

    [Fact]
    public void BuildExecutionGraph_EmptySteps_ShouldReturnFailure()
    {
        var command = new WorkflowGraphCommand("wf-1", "Empty", "1.0.0", new List<WorkflowGraphStepInput>());

        var result = _engine.BuildExecutionGraph(command);

        Assert.False(result.Success);
        Assert.Contains("at least one step", result.ErrorMessage);
    }

    [Fact]
    public void BuildExecutionGraph_DuplicateStepId_ShouldReturnFailure()
    {
        var command = new WorkflowGraphCommand("wf-1", "Dup", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("A", "Step A", "EngineA", new List<string>()),
            new("A", "Step A Copy", "EngineB", new List<string>())
        });

        var result = _engine.BuildExecutionGraph(command);

        Assert.False(result.Success);
        Assert.Contains("Duplicate step ID", result.ErrorMessage);
    }

    [Fact]
    public void BuildExecutionGraph_ParallelStepDetection_ShouldGroupCorrectly()
    {
        var command = new WorkflowGraphCommand("wf-1", "Parallel", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("Start", "Start", "StartEngine", new List<string>()),
            new("ParallelA", "Parallel A", "EngineA", new List<string> { "Start" }),
            new("ParallelB", "Parallel B", "EngineB", new List<string> { "Start" }),
            new("ParallelC", "Parallel C", "EngineC", new List<string> { "Start" }),
            new("Join", "Join", "JoinEngine", new List<string> { "ParallelA", "ParallelB", "ParallelC" })
        });

        var result = _engine.BuildExecutionGraph(command);

        Assert.True(result.Success);
        var groups = result.Graph!.ParallelGroups;
        Assert.Equal(3, groups.Count);
        Assert.Single(groups[0]);
        Assert.Equal(3, groups[1].Count);
        Assert.Single(groups[2]);
    }

    // 12. Deterministic Graph Generation
    [Fact]
    public void BuildExecutionGraph_DeterministicGeneration_SameInputProducesSameStructure()
    {
        var command = new WorkflowGraphCommand("wf-1", "Deterministic", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("A", "Step A", "EngineA", new List<string>()),
            new("B", "Step B", "EngineB", new List<string> { "A" }),
            new("C", "Step C", "EngineC", new List<string> { "A" }),
            new("D", "Step D", "EngineD", new List<string> { "B", "C" })
        });

        var result1 = _engine.BuildExecutionGraph(command);
        var result2 = _engine.BuildExecutionGraph(command);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Graph!.Nodes.Count, result2.Graph!.Nodes.Count);
        Assert.Equal(result1.Graph.Edges.Count, result2.Graph.Edges.Count);
        Assert.Equal(result1.Graph.ExecutionOrder, result2.Graph.ExecutionOrder);
        Assert.Equal(result1.Graph.ParallelGroups.Count, result2.Graph.ParallelGroups.Count);

        for (var i = 0; i < result1.Graph.ParallelGroups.Count; i++)
            Assert.Equal(result1.Graph.ParallelGroups[i], result2.Graph.ParallelGroups[i]);
    }

    // 13. Large Workflow Graph (100+ steps)
    [Fact]
    public void BuildExecutionGraph_LargeWorkflow_ShouldHandleCorrectly()
    {
        var steps = new List<WorkflowGraphStepInput>();

        for (var i = 0; i < 120; i++)
        {
            var deps = i == 0 ? new List<string>() : new List<string> { $"step_{i - 1}" };
            steps.Add(new WorkflowGraphStepInput($"step_{i}", $"Step {i}", $"Engine{i}", deps));
        }

        var command = new WorkflowGraphCommand("wf-large", "LargeWorkflow", "1.0.0", steps);

        var result = _engine.BuildExecutionGraph(command);

        Assert.True(result.Success);
        Assert.Equal(120, result.Graph!.Nodes.Count);
        Assert.Equal(119, result.Graph.Edges.Count);
        Assert.Equal(120, result.Graph.ExecutionOrder.Count);
        Assert.Equal("step_0", result.Graph.ExecutionOrder[0]);
        Assert.Equal("step_119", result.Graph.ExecutionOrder[119]);
    }

    [Fact]
    public void BuildExecutionGraph_LargeParallelWorkflow_ShouldDetectParallelism()
    {
        var steps = new List<WorkflowGraphStepInput>
        {
            new("root", "Root", "RootEngine", new List<string>())
        };

        for (var i = 0; i < 100; i++)
            steps.Add(new WorkflowGraphStepInput($"parallel_{i}", $"Parallel {i}", $"Engine{i}", new List<string> { "root" }));

        var joinDeps = Enumerable.Range(0, 100).Select(i => $"parallel_{i}").ToList();
        steps.Add(new WorkflowGraphStepInput("join", "Join", "JoinEngine", joinDeps));

        var command = new WorkflowGraphCommand("wf-wide", "WideWorkflow", "1.0.0", steps);

        var result = _engine.BuildExecutionGraph(command);

        Assert.True(result.Success);
        Assert.Equal(102, result.Graph!.Nodes.Count);
        Assert.Equal(3, result.Graph.ParallelGroups.Count);
        Assert.Single(result.Graph.ParallelGroups[0]);
        Assert.Equal(100, result.Graph.ParallelGroups[1].Count);
        Assert.Single(result.Graph.ParallelGroups[2]);
    }

    // 14. Concurrency Safety
    [Fact]
    public async Task BuildExecutionGraph_ConcurrentExecution_ShouldBeSafe()
    {
        var command = new WorkflowGraphCommand("wf-1", "Concurrent", "1.0.0", new List<WorkflowGraphStepInput>
        {
            new("A", "Step A", "EngineA", new List<string>()),
            new("B", "Step B", "EngineB", new List<string> { "A" }),
            new("C", "Step C", "EngineC", new List<string> { "B" })
        });

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => _engine.BuildExecutionGraph(command)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            Assert.True(result.Success);
            Assert.Equal(3, result.Graph!.Nodes.Count);
            Assert.Equal(2, result.Graph.Edges.Count);
            Assert.Equal(new[] { "A", "B", "C" }, result.Graph.ExecutionOrder.ToArray());
        }
    }
}
