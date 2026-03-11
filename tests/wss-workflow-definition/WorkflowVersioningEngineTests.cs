using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.System.Midstream.WSS.Stores;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowVersioningEngineTests
{
    private readonly WorkflowDefinitionStore _definitionStore;
    private readonly WorkflowVersionStore _versionStore;
    private readonly WorkflowVersioningEngine _engine;

    public WorkflowVersioningEngineTests()
    {
        _definitionStore = new WorkflowDefinitionStore();
        _versionStore = new WorkflowVersionStore();
        _engine = new WorkflowVersioningEngine(_versionStore, _definitionStore);

        var defEngine = new WorkflowDefinitionEngine(_definitionStore);
        defEngine.RegisterWorkflow("wf-ride", "Taxi Ride", "Ride flow", 1, new List<WorkflowStep>
        {
            new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
            new("step-2", "Complete", "PaymentEngine", new List<string>())
        });
    }

    [Fact]
    public void CreateVersion_ShouldCreateDraftVersion()
    {
        var result = _engine.CreateVersion("wf-ride", 1);

        Assert.Equal("wf-ride", result.WorkflowId);
        Assert.Equal(1, result.Version);
        Assert.Equal(WorkflowVersionStatus.Draft, result.Status);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateVersion_DuplicateVersion_ShouldThrow()
    {
        _engine.CreateVersion("wf-ride", 1);

        Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateVersion("wf-ride", 1));
    }

    [Fact]
    public void CreateVersion_MissingDefinition_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.CreateVersion("nonexistent", 1));
    }

    [Fact]
    public void ActivateVersion_ShouldSetActive()
    {
        _engine.CreateVersion("wf-ride", 1);

        var result = _engine.ActivateVersion("wf-ride", 1);

        Assert.Equal(WorkflowVersionStatus.Active, result.Status);
    }

    [Fact]
    public void ActivateVersion_ShouldSupersedePrevious()
    {
        _engine.CreateVersion("wf-ride", 1);
        _engine.ActivateVersion("wf-ride", 1);
        _engine.CreateVersion("wf-ride", 2);

        _engine.ActivateVersion("wf-ride", 2);

        var versions = _engine.GetVersions("wf-ride");
        var v1 = versions.First(v => v.Version == 1);
        var v2 = versions.First(v => v.Version == 2);
        Assert.Equal(WorkflowVersionStatus.Superseded, v1.Status);
        Assert.Equal(WorkflowVersionStatus.Active, v2.Status);
    }

    [Fact]
    public void GetActiveVersion_ShouldReturnActive()
    {
        _engine.CreateVersion("wf-ride", 1);
        _engine.ActivateVersion("wf-ride", 1);

        var result = _engine.GetActiveVersion("wf-ride");

        Assert.Equal(1, result.Version);
        Assert.Equal(WorkflowVersionStatus.Active, result.Status);
    }

    [Fact]
    public void GetActiveVersion_NoActive_ShouldThrow()
    {
        _engine.CreateVersion("wf-ride", 1);

        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetActiveVersion("wf-ride"));
    }

    [Fact]
    public void GetVersions_ShouldReturnAllOrdered()
    {
        _engine.CreateVersion("wf-ride", 2);
        _engine.CreateVersion("wf-ride", 1);
        _engine.CreateVersion("wf-ride", 3);

        var results = _engine.GetVersions("wf-ride");

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Version);
        Assert.Equal(2, results[1].Version);
        Assert.Equal(3, results[2].Version);
    }
}
