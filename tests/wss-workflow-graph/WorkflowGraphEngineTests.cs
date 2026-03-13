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
}
