using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Stores;
using WfDefinition = Whycespace.System.Midstream.WSS.Models.WorkflowDefinition;

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
        var result = _engine.RegisterWorkflowDefinition("wf-1", "Taxi Ride Request", "End-to-end ride flow", "1.0.0", SampleSteps());

        Assert.Equal("wf-1", result.WorkflowId);
        Assert.Equal("Taxi Ride Request", result.Name);
        Assert.Equal("End-to-end ride flow", result.Description);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal(3, result.Steps.Count);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterWorkflow_DuplicateId_ShouldThrow()
    {
        _engine.RegisterWorkflowDefinition("wf-1", "Taxi Ride Request", "Ride flow", "1.0.0", SampleSteps());

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterWorkflowDefinition("wf-1", "Duplicate", "Should fail", "1.0.0", SampleSteps()));
    }

    [Fact]
    public void GetWorkflow_ShouldReturnRegistered()
    {
        _engine.RegisterWorkflowDefinition("wf-1", "Taxi Ride Request", "Ride flow", "1.0.0", SampleSteps());

        var result = _engine.GetWorkflowDefinition("wf-1");

        Assert.Equal("wf-1", result.WorkflowId);
        Assert.Equal("Taxi Ride Request", result.Name);
    }

    [Fact]
    public void GetWorkflow_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _engine.GetWorkflowDefinition("nonexistent"));
    }

    [Fact]
    public void ListWorkflows_ShouldReturnAll()
    {
        _engine.RegisterWorkflowDefinition("wf-1", "Taxi Ride Request", "Ride flow", "1.0.0", SampleSteps());
        _engine.RegisterWorkflowDefinition("wf-2", "Property Letting Onboarding", "Letting flow", "1.0.0", SampleSteps());
        _engine.RegisterWorkflowDefinition("wf-3", "SPV Capital Contribution", "Capital flow", "1.0.0", SampleSteps());

        var results = _engine.ListWorkflowDefinitions();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void ListWorkflows_Empty_ShouldReturnEmpty()
    {
        var results = _engine.ListWorkflowDefinitions();

        Assert.Empty(results);
    }

    [Fact]
    public void ValidateWorkflowDefinition_ValidWorkflow_ShouldReturnNoViolations()
    {
        var definition = new WfDefinition("wf-1", "Valid Workflow", "Test", "1.0.0", SampleSteps(), DateTimeOffset.UtcNow);

        var violations = _engine.ValidateWorkflowDefinition(definition);

        Assert.Empty(violations);
    }

    [Fact]
    public void ValidateWorkflowDefinition_DuplicateStepIds_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-2" }),
            new("step-1", "Duplicate", "EngineB", new List<string>()),
            new("step-2", "Second", "EngineC", new List<string>())
        };
        var definition = new WfDefinition("wf-1", "Dup Steps", "Test", "1.0.0",steps, DateTimeOffset.UtcNow);

        var violations = _engine.ValidateWorkflowDefinition(definition);

        Assert.Contains(violations, v => v.Contains("Duplicate step ID: 'step-1'"));
    }

    [Fact]
    public void ValidateWorkflowDefinition_InvalidGraphReference_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-99" }),
            new("step-2", "End", "EngineB", new List<string>())
        };
        var definition = new WfDefinition("wf-1", "Bad Ref", "Test", "1.0.0",steps, DateTimeOffset.UtcNow);

        var violations = _engine.ValidateWorkflowDefinition(definition);

        Assert.Contains(violations, v => v.Contains("NextStep 'step-99' does not exist"));
    }

    [Fact]
    public void ValidateWorkflowDefinition_CircularDependency_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-2" }),
            new("step-2", "Second", "EngineB", new List<string> { "step-3" }),
            new("step-3", "Third", "EngineC", new List<string> { "step-1" })
        };
        var definition = new WfDefinition("wf-1", "Circular", "Test", "1.0.0",steps, DateTimeOffset.UtcNow);

        var violations = _engine.ValidateWorkflowDefinition(definition);

        Assert.Contains(violations, v => v.Contains("Circular dependency"));
    }

    [Fact]
    public void ValidateWorkflowDefinition_EmptySteps_ShouldDetect()
    {
        var definition = new WfDefinition("wf-1", "Empty", "Test", "1.0.0",new List<WorkflowStep>(), DateTimeOffset.UtcNow);

        var violations = _engine.ValidateWorkflowDefinition(definition);

        Assert.Contains(violations, v => v.Contains("at least one step"));
    }
}
