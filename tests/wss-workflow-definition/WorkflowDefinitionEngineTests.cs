using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS;
using Whycespace.System.Midstream.WSS.Stores;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowDefinitionEngineTests
{
    private readonly WorkflowDefinitionStore _store;
    private readonly WorkflowDefinitionEngine _engine;

    public WorkflowDefinitionEngineTests()
    {
        _store = new WorkflowDefinitionStore();
        _engine = new WorkflowDefinitionEngine(_store);
    }

    private static IReadOnlyList<WorkflowStep> SampleSteps() => new List<WorkflowStep>
    {
        new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
        new("step-2", "Match", "DriverMatchEngine", new List<string> { "step-3" }),
        new("step-3", "Complete", "PaymentEngine", new List<string>())
    };

    [Fact]
    public void RegisterWorkflow_ShouldStoreAndReturn()
    {
        var result = _engine.RegisterWorkflow("wf-1", "Taxi Ride Request", "End-to-end ride flow", 1, SampleSteps());

        Assert.Equal("wf-1", result.WorkflowId);
        Assert.Equal("Taxi Ride Request", result.Name);
        Assert.Equal("End-to-end ride flow", result.Description);
        Assert.Equal(1, result.Version);
        Assert.Equal(3, result.Steps.Count);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterWorkflow_DuplicateId_ShouldThrow()
    {
        _engine.RegisterWorkflow("wf-1", "Taxi Ride Request", "Ride flow", 1, SampleSteps());

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterWorkflow("wf-1", "Duplicate", "Should fail", 1, SampleSteps()));
    }

    [Fact]
    public void GetWorkflow_ShouldReturnRegistered()
    {
        _engine.RegisterWorkflow("wf-1", "Taxi Ride Request", "Ride flow", 1, SampleSteps());

        var result = _engine.GetWorkflow("wf-1");

        Assert.Equal("wf-1", result.WorkflowId);
        Assert.Equal("Taxi Ride Request", result.Name);
    }

    [Fact]
    public void GetWorkflow_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _engine.GetWorkflow("nonexistent"));
    }

    [Fact]
    public void ListWorkflows_ShouldReturnAll()
    {
        _engine.RegisterWorkflow("wf-1", "Taxi Ride Request", "Ride flow", 1, SampleSteps());
        _engine.RegisterWorkflow("wf-2", "Property Letting Onboarding", "Letting flow", 1, SampleSteps());
        _engine.RegisterWorkflow("wf-3", "SPV Capital Contribution", "Capital flow", 1, SampleSteps());

        var results = _engine.ListWorkflows();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ListWorkflows_Empty_ShouldReturnEmpty()
    {
        var results = _engine.ListWorkflows();

        Assert.Empty(results);
    }
}
