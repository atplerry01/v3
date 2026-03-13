using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Dependency;
using Whycespace.Engines.T1M.WSS.Stores;
using WfDefinition = Whycespace.System.Midstream.WSS.Models.WorkflowDefinition;

namespace Whycespace.WSS.WorkflowDependency.Tests;

public class WorkflowDependencyEngineTests
{
    private readonly WorkflowDefinitionStore _definitionStore;
    private readonly WorkflowDependencyAnalyzer _engine;

    public WorkflowDependencyEngineTests()
    {
        _definitionStore = new WorkflowDefinitionStore();
        _engine = new WorkflowDependencyAnalyzer(_definitionStore);
    }

    private static IReadOnlyList<WorkflowStep> LinearSteps() => new List<WorkflowStep>
    {
        new("validate-passenger", "ValidatePassenger", "ValidationEngine", new List<string> { "find-driver" }),
        new("find-driver", "FindDriver", "MatchEngine", new List<string> { "create-ride" }),
        new("create-ride", "CreateRide", "RideEngine", new List<string>())
    };

    private static IReadOnlyList<WorkflowStep> BranchingSteps() => new List<WorkflowStep>
    {
        new("start", "Start", "InitEngine", new List<string> { "branch-a", "branch-b" }),
        new("branch-a", "BranchA", "EngineA", new List<string> { "merge" }),
        new("branch-b", "BranchB", "EngineB", new List<string> { "merge" }),
        new("merge", "Merge", "MergeEngine", new List<string> { "end" }),
        new("end", "End", "FinalEngine", new List<string>())
    };

    // 1. Linear workflow dependency analysis
    [Fact]
    public void AnalyzeWorkflowDependencies_LinearWorkflow_ShouldMapDependencies()
    {
        var workflow = new WfDefinition("wf-1", "Ride Request", "Test", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);

        var result = _engine.AnalyzeWorkflowDependencies(workflow);

        Assert.Equal("wf-1", result.WorkflowId);
        Assert.False(result.HasIssues);

        // validate-passenger has no dependencies (start node)
        Assert.Empty(result.Dependencies["validate-passenger"]);

        // find-driver depends on validate-passenger
        Assert.Contains("validate-passenger", result.Dependencies["find-driver"]);

        // create-ride depends on find-driver
        Assert.Contains("find-driver", result.Dependencies["create-ride"]);
    }

    // 2. Branching workflow dependencies
    [Fact]
    public void AnalyzeWorkflowDependencies_BranchingWorkflow_ShouldMapDependencies()
    {
        var workflow = new WfDefinition("wf-2", "Branching", "Test", "1.0.0", BranchingSteps(), DateTimeOffset.UtcNow);

        var result = _engine.AnalyzeWorkflowDependencies(workflow);

        Assert.False(result.HasIssues);

        // merge depends on both branches
        Assert.Contains("branch-a", result.Dependencies["merge"]);
        Assert.Contains("branch-b", result.Dependencies["merge"]);
        Assert.Equal(2, result.Dependencies["merge"].Count);
    }

    // 3. Execution order resolution
    [Fact]
    public void ResolveExecutionOrder_LinearWorkflow_ShouldReturnCorrectOrder()
    {
        var workflow = new WfDefinition("wf-1", "Ride Request", "Test", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);

        var order = _engine.ResolveExecutionOrder(workflow);

        Assert.Equal(3, order.Count);
        Assert.Equal("validate-passenger", order[0]);
        Assert.Equal("find-driver", order[1]);
        Assert.Equal("create-ride", order[2]);
    }

    [Fact]
    public void ResolveExecutionOrder_BranchingWorkflow_ShouldRespectDependencies()
    {
        var workflow = new WfDefinition("wf-2", "Branching", "Test", "1.0.0", BranchingSteps(), DateTimeOffset.UtcNow);

        var order = _engine.ResolveExecutionOrder(workflow);

        Assert.Equal(5, order.Count);

        // start must come first
        Assert.Equal("start", order[0]);

        // both branches before merge
        var orderList = order.ToList();
        var mergeIndex = orderList.IndexOf("merge");
        var branchAIndex = orderList.IndexOf("branch-a");
        var branchBIndex = orderList.IndexOf("branch-b");
        Assert.True(branchAIndex < mergeIndex);
        Assert.True(branchBIndex < mergeIndex);

        // end must come last
        Assert.Equal("end", order[^1]);
    }

    // 4. Missing dependency detection
    [Fact]
    public void AnalyzeWorkflowDependencies_MissingDependency_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-99" }),
            new("step-2", "End", "EngineB", new List<string>())
        };
        var workflow = new WfDefinition("wf-1", "Missing Ref", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var result = _engine.AnalyzeWorkflowDependencies(workflow);

        Assert.True(result.HasIssues);
        Assert.NotEmpty(result.MissingDependencies);
        Assert.Contains(result.MissingDependencies, m => m.Contains("step-99"));
    }

    // 5. Circular dependency detection
    [Fact]
    public void DetectCircularDependencies_CircularWorkflow_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-2" }),
            new("step-2", "Second", "EngineB", new List<string> { "step-3" }),
            new("step-3", "Third", "EngineC", new List<string> { "step-1" })
        };
        var workflow = new WfDefinition("wf-1", "Circular", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var circular = _engine.DetectCircularDependencies(workflow);

        Assert.NotEmpty(circular);
    }

    [Fact]
    public void DetectCircularDependencies_AcyclicWorkflow_ShouldReturnEmpty()
    {
        var workflow = new WfDefinition("wf-1", "Linear", "Test", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);

        var circular = _engine.DetectCircularDependencies(workflow);

        Assert.Empty(circular);
    }

    // 6. External workflow dependency detection
    [Fact]
    public void GetExternalWorkflowDependencies_WithExternalRef_ShouldDetect()
    {
        // Register an external workflow
        var externalSteps = new List<WorkflowStep>
        {
            new("ext-1", "Check", "CheckEngine", new List<string>())
        };
        _definitionStore.Register(new WfDefinition(
            "DriverAvailabilityWorkflow", "Driver Availability", "External", "1.0.0", externalSteps, DateTimeOffset.UtcNow));

        // Workflow referencing external workflow as an engine
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Validate", "ValidationEngine", new List<string> { "step-2" }),
            new("step-2", "CheckDrivers", "DriverAvailabilityWorkflow", new List<string> { "step-3" }),
            new("step-3", "Complete", "RideEngine", new List<string>())
        };
        var workflow = new WfDefinition("wf-taxi", "Taxi Ride", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var external = _engine.GetExternalWorkflowDependencies(workflow);

        Assert.Single(external);
        Assert.Contains("DriverAvailabilityWorkflow", external);
    }

    [Fact]
    public void GetExternalWorkflowDependencies_WithWorkflowSuffix_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "InitEngine", new List<string> { "step-2" }),
            new("step-2", "SubProcess", "PaymentWorkflow", new List<string>())
        };
        var workflow = new WfDefinition("wf-order", "Order", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var external = _engine.GetExternalWorkflowDependencies(workflow);

        Assert.Single(external);
        Assert.Contains("PaymentWorkflow", external);
    }

    [Fact]
    public void GetExternalWorkflowDependencies_NoExternalRefs_ShouldReturnEmpty()
    {
        var workflow = new WfDefinition("wf-1", "Simple", "Test", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);

        var external = _engine.GetExternalWorkflowDependencies(workflow);

        Assert.Empty(external);
    }

    // Full analysis integration
    [Fact]
    public void AnalyzeWorkflowDependencies_CircularWorkflow_ShouldReportCircular()
    {
        var steps = new List<WorkflowStep>
        {
            new("a", "A", "EngineA", new List<string> { "b" }),
            new("b", "B", "EngineB", new List<string> { "a" })
        };
        var workflow = new WfDefinition("wf-1", "Cycle", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var result = _engine.AnalyzeWorkflowDependencies(workflow);

        Assert.True(result.HasIssues);
        Assert.NotEmpty(result.CircularDependencies);
    }

    [Fact]
    public void ResolveExecutionOrder_EmptyWorkflow_ShouldReturnEmpty()
    {
        var workflow = new WfDefinition("wf-1", "Empty", "Test", "1.0.0", new List<WorkflowStep>(), DateTimeOffset.UtcNow);

        var order = _engine.ResolveExecutionOrder(workflow);

        Assert.Empty(order);
    }
}
