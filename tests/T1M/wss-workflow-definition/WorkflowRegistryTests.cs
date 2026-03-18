using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Registry;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowDefinition;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowRegistryTests
{
    private readonly WorkflowRegistry _registry;

    public WorkflowRegistryTests()
    {
        _registry = new WorkflowRegistry();
    }

    private static WfDefinition CreateDefinition(string workflowId = "wf-ride", string name = "Taxi Ride")
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
            new("step-2", "Match", "DriverMatchEngine", new List<string> { "step-3" }),
            new("step-3", "Complete", "PaymentEngine", new List<string>())
        };

        return new WfDefinition(workflowId, name, "End-to-end ride flow", "1.0.0", steps, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterWorkflow_ShouldStoreAndRetrieve()
    {
        var definition = CreateDefinition();

        _registry.RegisterWorkflow(definition);

        var result = _registry.GetWorkflow("wf-ride");
        Assert.Equal("wf-ride", result.WorkflowId);
        Assert.Equal("Taxi Ride", result.Name);
        Assert.Equal(3, result.Steps.Count);
    }

    [Fact]
    public void GetWorkflow_ShouldReturnRegistered()
    {
        _registry.RegisterWorkflow(CreateDefinition());

        var result = _registry.GetWorkflow("wf-ride");

        Assert.Equal("wf-ride", result.WorkflowId);
        Assert.Equal("Taxi Ride", result.Name);
        Assert.Equal("End-to-end ride flow", result.Description);
        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public void ListWorkflows_ShouldReturnAll()
    {
        _registry.RegisterWorkflow(CreateDefinition("wf-1", "Workflow 1"));
        _registry.RegisterWorkflow(CreateDefinition("wf-2", "Workflow 2"));
        _registry.RegisterWorkflow(CreateDefinition("wf-3", "Workflow 3"));

        var results = _registry.ListWorkflows();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void RegisterWorkflow_DuplicateId_ShouldThrow()
    {
        _registry.RegisterWorkflow(CreateDefinition());

        Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterWorkflow(CreateDefinition()));
    }

    [Fact]
    public void WorkflowExists_ShouldReturnTrue_WhenRegistered()
    {
        _registry.RegisterWorkflow(CreateDefinition());

        Assert.True(_registry.WorkflowExists("wf-ride"));
    }

    [Fact]
    public void WorkflowExists_ShouldReturnFalse_WhenNotRegistered()
    {
        Assert.False(_registry.WorkflowExists("nonexistent"));
    }

    [Fact]
    public void RemoveWorkflow_ShouldRemoveRegistered()
    {
        _registry.RegisterWorkflow(CreateDefinition());

        _registry.RemoveWorkflow("wf-ride");

        Assert.False(_registry.WorkflowExists("wf-ride"));
        Assert.Throws<KeyNotFoundException>(() => _registry.GetWorkflow("wf-ride"));
    }

    [Fact]
    public void RemoveWorkflow_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _registry.RemoveWorkflow("nonexistent"));
    }

    [Fact]
    public void GetWorkflow_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _registry.GetWorkflow("nonexistent"));
    }

    [Fact]
    public void RegisterWorkflow_InvalidDefinition_EmptySteps_ShouldThrow()
    {
        var definition = new WfDefinition("wf-bad", "Bad", "No steps", "1.0.0",
            new List<WorkflowStep>(), DateTimeOffset.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() => _registry.RegisterWorkflow(definition));
        Assert.Contains("at least one step", ex.Message);
    }

    [Fact]
    public void RegisterWorkflow_InvalidDefinition_CircularDependency_ShouldThrow()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-2" }),
            new("step-2", "Second", "EngineB", new List<string> { "step-1" })
        };

        var definition = new WfDefinition("wf-cycle", "Cyclic", "Has cycle", "1.0.0", steps, DateTimeOffset.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() => _registry.RegisterWorkflow(definition));
        Assert.Contains("Circular dependency", ex.Message);
    }

    [Fact]
    public void RegisterWorkflow_InvalidDefinition_DuplicateStepIds_ShouldThrow()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string>()),
            new("step-1", "Duplicate", "EngineB", new List<string>())
        };

        var definition = new WfDefinition("wf-dup", "Dups", "Has dups", "1.0.0", steps, DateTimeOffset.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() => _registry.RegisterWorkflow(definition));
        Assert.Contains("Duplicate step ID", ex.Message);
    }
}
