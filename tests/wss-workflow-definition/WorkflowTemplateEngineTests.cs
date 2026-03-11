using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS;
using Whycespace.System.Midstream.WSS.Stores;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowTemplateEngineTests
{
    private readonly WorkflowDefinitionStore _definitionStore;
    private readonly WorkflowTemplateStore _templateStore;
    private readonly WorkflowTemplateEngine _engine;

    public WorkflowTemplateEngineTests()
    {
        _definitionStore = new WorkflowDefinitionStore();
        _templateStore = new WorkflowTemplateStore();
        _engine = new WorkflowTemplateEngine(_templateStore, _definitionStore);

        var defEngine = new WorkflowDefinitionEngine(_definitionStore);
        defEngine.RegisterWorkflow("wf-ride", "Taxi Ride", "Ride flow", 1, new List<WorkflowStep>
        {
            new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
            new("step-2", "Complete", "PaymentEngine", new List<string>())
        });
    }

    [Fact]
    public void CreateTemplate_ShouldStoreAndReturn()
    {
        var parameters = new Dictionary<string, string> { ["region"] = "london", ["currency"] = "GBP" };

        var result = _engine.CreateTemplate("tmpl-1", "wf-ride", parameters);

        Assert.Equal("tmpl-1", result.TemplateId);
        Assert.Equal("wf-ride", result.WorkflowDefinitionId);
        Assert.Equal("london", result.Parameters["region"]);
        Assert.Equal("GBP", result.Parameters["currency"]);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateTemplate_DuplicateId_ShouldThrow()
    {
        var parameters = new Dictionary<string, string> { ["region"] = "london" };
        _engine.CreateTemplate("tmpl-1", "wf-ride", parameters);

        Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateTemplate("tmpl-1", "wf-ride", parameters));
    }

    [Fact]
    public void CreateTemplate_InvalidDefinition_ShouldThrow()
    {
        var parameters = new Dictionary<string, string> { ["region"] = "london" };

        Assert.Throws<KeyNotFoundException>(() =>
            _engine.CreateTemplate("tmpl-1", "nonexistent", parameters));
    }

    [Fact]
    public void GetTemplate_ShouldReturnRegistered()
    {
        var parameters = new Dictionary<string, string> { ["region"] = "london" };
        _engine.CreateTemplate("tmpl-1", "wf-ride", parameters);

        var result = _engine.GetTemplate("tmpl-1");

        Assert.Equal("tmpl-1", result.TemplateId);
        Assert.Equal("wf-ride", result.WorkflowDefinitionId);
    }

    [Fact]
    public void GetTemplate_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _engine.GetTemplate("nonexistent"));
    }

    [Fact]
    public void ListTemplates_ShouldReturnAll()
    {
        var parameters = new Dictionary<string, string> { ["region"] = "london" };
        _engine.CreateTemplate("tmpl-1", "wf-ride", parameters);
        _engine.CreateTemplate("tmpl-2", "wf-ride", new Dictionary<string, string> { ["region"] = "manchester" });
        _engine.CreateTemplate("tmpl-3", "wf-ride", new Dictionary<string, string> { ["region"] = "birmingham" });

        var results = _engine.ListTemplates();

        Assert.Equal(3, results.Count);
    }
}
