using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Versioning;
using Whycespace.Engines.T1M.WSS.Stores;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowVersioningEngineTests
{
    private readonly WorkflowVersionStore _versionStore;
    private readonly WorkflowVersioningEngine _engine;

    public WorkflowVersioningEngineTests()
    {
        _versionStore = new WorkflowVersionStore();
        _engine = new WorkflowVersioningEngine(_versionStore);
    }

    private static WfDefinition CreateWorkflow(string workflowId, string version)
    {
        return new WfDefinition(
            workflowId,
            "Test Workflow",
            "A test workflow",
            version,
            new List<WorkflowStep>
            {
                new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
                new("step-2", "Complete", "PaymentEngine", new List<string>())
            },
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterWorkflowVersion_ShouldStoreAndReturn()
    {
        var workflow = CreateWorkflow("wf-ride", "1.0.0");

        var result = _engine.RegisterWorkflowVersion(workflow);

        Assert.Equal("wf-ride", result.WorkflowId);
        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public void GetWorkflowVersion_ShouldReturnSpecificVersion()
    {
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.1.0"));

        var result = _engine.GetWorkflowVersion("wf-ride", "1.0.0");

        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public void GetLatestWorkflow_ShouldReturnHighestSemanticVersion()
    {
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.2.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.1.0"));

        var result = _engine.GetLatestWorkflow("wf-ride");

        Assert.Equal("1.2.0", result.Version);
    }

    [Fact]
    public void RegisterWorkflowVersion_DuplicateVersion_ShouldThrow()
    {
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0"));

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0")));
    }

    [Fact]
    public void RegisterWorkflowVersion_InvalidFormat_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "v1")));
    }

    [Fact]
    public void ListWorkflowVersions_ShouldReturnAllOrdered()
    {
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "2.0.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.1.0"));

        var results = _engine.ListWorkflowVersions("wf-ride");

        Assert.Equal(3, results.Count);
        Assert.Equal("1.0.0", results[0].Version);
        Assert.Equal("1.1.0", results[1].Version);
        Assert.Equal("2.0.0", results[2].Version);
    }

    [Fact]
    public void WorkflowVersionExists_ShouldReturnCorrectResult()
    {
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0"));

        Assert.True(_engine.WorkflowVersionExists("wf-ride", "1.0.0"));
        Assert.False(_engine.WorkflowVersionExists("wf-ride", "2.0.0"));
        Assert.False(_engine.WorkflowVersionExists("nonexistent", "1.0.0"));
    }

    [Fact]
    public void GetWorkflowVersion_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetWorkflowVersion("wf-ride", "1.0.0"));
    }

    [Fact]
    public void GetLatestWorkflow_NoVersions_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetLatestWorkflow("wf-ride"));
    }

    [Fact]
    public void GetLatestWorkflow_ShouldResolveSemanticOrder()
    {
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "1.0.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "10.0.0"));
        _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "2.0.0"));

        var result = _engine.GetLatestWorkflow("wf-ride");

        Assert.Equal("10.0.0", result.Version);
    }

    [Fact]
    public void RegisterWorkflowVersion_EmptyWorkflowId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.RegisterWorkflowVersion(CreateWorkflow("", "1.0.0")));
    }

    [Fact]
    public void RegisterWorkflowVersion_EmptyVersion_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.RegisterWorkflowVersion(CreateWorkflow("wf-ride", "")));
    }

    [Fact]
    public void ListWorkflowVersions_Empty_ShouldReturnEmpty()
    {
        var results = _engine.ListWorkflowVersions("nonexistent");

        Assert.Empty(results);
    }
}
