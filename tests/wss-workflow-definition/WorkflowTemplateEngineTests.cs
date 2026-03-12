using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.Engines.T1M.WSS.Stores;
using WssWorkflowGraph = Whycespace.System.Midstream.WSS.Models.WorkflowGraph;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

public class WorkflowTemplateEngineTests
{
    private readonly WorkflowTemplateStore _templateStore;
    private readonly WorkflowTemplateEngine _engine;

    public WorkflowTemplateEngineTests()
    {
        _templateStore = new WorkflowTemplateStore();
        var graphEngine = new WorkflowGraphEngine();
        _engine = new WorkflowTemplateEngine(_templateStore, graphEngine);
    }

    private static WorkflowTemplate CreateMobilityTemplate(string templateId = "tmpl-mobility")
    {
        var steps = new List<WorkflowTemplateStep>
        {
            new("step-request", "Request ${cluster} ride", "${cluster}Engine",
                "${cluster}.${subcluster}.createRide",
                new Dictionary<string, string> { ["cluster"] = "mobility", ["subcluster"] = "taxi" },
                null),
            new("step-match", "Match driver", "${cluster}MatchEngine",
                "${cluster}.${subcluster}.matchDriver",
                new Dictionary<string, string>(),
                null),
            new("step-complete", "Complete trip", "PaymentEngine",
                "${cluster}.${subcluster}.completeTrip",
                new Dictionary<string, string>(),
                new WorkflowFailurePolicy(FailureAction.Retry, 3, null))
        };

        var transitions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["step-request"] = new List<string> { "step-match" },
            ["step-match"] = new List<string> { "step-complete" },
            ["step-complete"] = new List<string>()
        };

        var graph = new WssWorkflowGraph(templateId, transitions);

        return new WorkflowTemplate(templateId, "${cluster} ${subcluster} workflow", 1,
            "Workflow for ${cluster} ${subcluster} operations", steps, graph);
    }

    [Fact]
    public void RegisterTemplate_ShouldStoreAndRetrieve()
    {
        var template = CreateMobilityTemplate();

        _engine.RegisterTemplate(template);

        var result = _engine.GetTemplate("tmpl-mobility");
        Assert.Equal("tmpl-mobility", result.TemplateId);
        Assert.Equal(3, result.Steps.Count);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public void GetTemplate_ShouldReturnRegistered()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate());

        var result = _engine.GetTemplate("tmpl-mobility");

        Assert.Equal("tmpl-mobility", result.TemplateId);
        Assert.Equal("${cluster} ${subcluster} workflow", result.Name);
    }

    [Fact]
    public void GetTemplate_NotFound_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _engine.GetTemplate("nonexistent"));
    }

    [Fact]
    public void ListTemplates_ShouldReturnAll()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate("tmpl-1"));
        _engine.RegisterTemplate(CreateMobilityTemplate("tmpl-2"));
        _engine.RegisterTemplate(CreateMobilityTemplate("tmpl-3"));

        var results = _engine.ListTemplates();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void GenerateWorkflowDefinition_ShouldSubstituteParameters()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate());

        var parameters = new Dictionary<string, string>
        {
            ["cluster"] = "mobility",
            ["subcluster"] = "taxi",
            ["workflowId"] = "wf-taxi-001"
        };

        var definition = _engine.GenerateWorkflowDefinition("tmpl-mobility", parameters);

        Assert.Equal("wf-taxi-001", definition.WorkflowId);
        Assert.Equal("mobility taxi workflow", definition.Name);
        Assert.Equal("Workflow for mobility taxi operations", definition.Description);
        Assert.Equal(3, definition.Steps.Count);

        var requestStep = definition.Steps.First(s => s.StepId == "step-request");
        Assert.Equal("mobility.taxi.createRide", requestStep.Name);
        Assert.Equal("mobilityEngine", requestStep.EngineName);

        var matchStep = definition.Steps.First(s => s.StepId == "step-match");
        Assert.Equal("mobility.taxi.matchDriver", matchStep.Name);
        Assert.Equal("mobilityMatchEngine", matchStep.EngineName);

        var completeStep = definition.Steps.First(s => s.StepId == "step-complete");
        Assert.Equal("mobility.taxi.completeTrip", completeStep.Name);
    }

    [Fact]
    public void GenerateWorkflowDefinition_ShouldPreserveGraphTransitions()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate());

        var parameters = new Dictionary<string, string>
        {
            ["cluster"] = "mobility",
            ["subcluster"] = "taxi"
        };

        var definition = _engine.GenerateWorkflowDefinition("tmpl-mobility", parameters);

        var requestStep = definition.Steps.First(s => s.StepId == "step-request");
        Assert.Single(requestStep.NextSteps);
        Assert.Equal("step-match", requestStep.NextSteps[0]);

        var completeStep = definition.Steps.First(s => s.StepId == "step-complete");
        Assert.Empty(completeStep.NextSteps);
    }

    [Fact]
    public void GenerateWorkflowDefinition_MissingParameter_ShouldThrow()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate());

        var parameters = new Dictionary<string, string>
        {
            ["cluster"] = "mobility"
            // missing "subcluster"
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            _engine.GenerateWorkflowDefinition("tmpl-mobility", parameters));

        Assert.Contains("subcluster", ex.Message);
    }

    [Fact]
    public void GenerateWorkflowDefinition_TemplateNotFound_ShouldThrow()
    {
        var parameters = new Dictionary<string, string> { ["cluster"] = "mobility" };

        Assert.Throws<KeyNotFoundException>(() =>
            _engine.GenerateWorkflowDefinition("nonexistent", parameters));
    }

    [Fact]
    public void RegisterTemplate_DuplicateId_ShouldThrow()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate());

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterTemplate(CreateMobilityTemplate()));
    }

    [Fact]
    public void RegisterTemplate_InvalidGraph_Cycle_ShouldThrow()
    {
        var steps = new List<WorkflowTemplateStep>
        {
            new("step-a", "Step A", "EngineA", "cmd-a", new Dictionary<string, string>(), null),
            new("step-b", "Step B", "EngineB", "cmd-b", new Dictionary<string, string>(), null)
        };

        var transitions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["step-a"] = new List<string> { "step-b" },
            ["step-b"] = new List<string> { "step-a" }
        };

        var graph = new WssWorkflowGraph("tmpl-cycle", transitions);
        var template = new WorkflowTemplate("tmpl-cycle", "Cycle Template", 1, "Has cycle", steps, graph);

        Assert.Throws<ArgumentException>(() => _engine.RegisterTemplate(template));
    }

    [Fact]
    public void RegisterTemplate_DuplicateStepIds_ShouldThrow()
    {
        var steps = new List<WorkflowTemplateStep>
        {
            new("step-a", "Step A", "EngineA", "cmd-a", new Dictionary<string, string>(), null),
            new("step-a", "Step A Dup", "EngineB", "cmd-b", new Dictionary<string, string>(), null)
        };

        var transitions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["step-a"] = new List<string>()
        };

        var graph = new WssWorkflowGraph("tmpl-dup", transitions);
        var template = new WorkflowTemplate("tmpl-dup", "Dup Steps", 1, "Has dups", steps, graph);

        var ex = Assert.Throws<ArgumentException>(() => _engine.RegisterTemplate(template));
        Assert.Contains("Duplicate step ID", ex.Message);
    }

    [Fact]
    public void GenerateWorkflowDefinition_PropertyDomain_ShouldSubstitute()
    {
        var steps = new List<WorkflowTemplateStep>
        {
            new("step-validate", "Validate listing", "${cluster}PolicyEngine",
                "${cluster}.${subcluster}.validateListing",
                new Dictionary<string, string>(), null),
            new("step-publish", "Publish listing", "${cluster}Engine",
                "${cluster}.${subcluster}.publishListing",
                new Dictionary<string, string>(), null)
        };

        var transitions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["step-validate"] = new List<string> { "step-publish" },
            ["step-publish"] = new List<string>()
        };

        var graph = new WssWorkflowGraph("tmpl-property", transitions);
        var template = new WorkflowTemplate("tmpl-property", "${cluster} workflow", 1,
            "Property workflow", steps, graph);

        _engine.RegisterTemplate(template);

        var parameters = new Dictionary<string, string>
        {
            ["cluster"] = "property",
            ["subcluster"] = "letting"
        };

        var definition = _engine.GenerateWorkflowDefinition("tmpl-property", parameters);

        Assert.Equal("property workflow", definition.Name);
        var validateStep = definition.Steps.First(s => s.StepId == "step-validate");
        Assert.Equal("property.letting.validateListing", validateStep.Name);
        Assert.Equal("propertyPolicyEngine", validateStep.EngineName);
    }

    [Fact]
    public void GenerateWorkflowDefinition_ShouldGenerateWorkflowIdWhenNotProvided()
    {
        _engine.RegisterTemplate(CreateMobilityTemplate());

        var parameters = new Dictionary<string, string>
        {
            ["cluster"] = "mobility",
            ["subcluster"] = "taxi"
        };

        var definition = _engine.GenerateWorkflowDefinition("tmpl-mobility", parameters);

        Assert.StartsWith("tmpl-mobility-", definition.WorkflowId);
    }
}
