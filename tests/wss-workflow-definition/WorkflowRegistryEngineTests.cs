using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.Engines.T1M.WSS.Stores;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowRegistryEngineTests
{
    private readonly WorkflowDefinitionStore _definitionStore;
    private readonly WorkflowRegistryStore _registryStore;
    private readonly WorkflowRegistryEngine _engine;

    public WorkflowRegistryEngineTests()
    {
        _definitionStore = new WorkflowDefinitionStore();
        _registryStore = new WorkflowRegistryStore();
        _engine = new WorkflowRegistryEngine(_registryStore, _definitionStore);

        var defEngine = new WorkflowDefinitionEngine(_definitionStore);
        defEngine.RegisterWorkflowDefinition("wf-ride", "Taxi Ride", "Ride flow", "1.0.0", new List<WorkflowStep>
        {
            new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
            new("step-2", "Complete", "PaymentEngine", new List<string>())
        });
        defEngine.RegisterWorkflowDefinition("wf-letting", "Property Letting", "Letting flow", "1.0.0", new List<WorkflowStep>
        {
            new("step-1", "Onboard", "OnboardEngine", new List<string>())
        });
    }

    [Fact]
    public void RegisterWorkflow_ShouldCreateRegistryEntry()
    {
        var result = _engine.RegisterWorkflow("wf-ride");

        Assert.Equal("wf-ride", result.WorkflowId);
        Assert.Equal("Taxi Ride", result.Name);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal(WorkflowRegistryStatus.Active, result.Status);
        Assert.True(result.RegisteredAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterWorkflow_DuplicateId_ShouldThrow()
    {
        _engine.RegisterWorkflow("wf-ride");

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterWorkflow("wf-ride"));
    }

    [Fact]
    public void RegisterWorkflow_MissingDefinition_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.RegisterWorkflow("nonexistent"));
    }

    [Fact]
    public void GetWorkflow_ShouldReturnRegistered()
    {
        _engine.RegisterWorkflow("wf-ride");

        var result = _engine.GetWorkflow("wf-ride");

        Assert.Equal("wf-ride", result.WorkflowId);
        Assert.Equal("Taxi Ride", result.Name);
    }

    [Fact]
    public void GetWorkflow_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _engine.GetWorkflow("nonexistent"));
    }

    [Fact]
    public void ListWorkflows_ShouldReturnAll()
    {
        _engine.RegisterWorkflow("wf-ride");
        _engine.RegisterWorkflow("wf-letting");

        var results = _engine.ListWorkflows();

        Assert.Equal(2, results.Count);
    }
}
