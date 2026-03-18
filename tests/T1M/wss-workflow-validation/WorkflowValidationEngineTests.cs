using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.Persistence.Workflow;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowDefinition;
using WfGraph = Whycespace.Systems.Midstream.WSS.Models.WorkflowGraph;
using WfTemplate = Whycespace.Systems.Midstream.WSS.Definition.WorkflowTemplate;
using WfTemplateStep = Whycespace.Systems.Midstream.WSS.Definition.WorkflowTemplateStep;

namespace Whycespace.WSS.WorkflowValidation.Tests;

public class WorkflowValidationEngineTests
{
    private readonly WorkflowValidationOrchestrator _engine;
    private readonly TemplateStoreAdapter _templateStore;
    private readonly VersionStoreAdapter _versionStore;

    public WorkflowValidationEngineTests()
    {
        _templateStore = new TemplateStoreAdapter();
        _versionStore = new VersionStoreAdapter();

        var graphEngine = new WorkflowGraphEngine();
        var definitionEngine = new WorkflowDefinitionEngine();
        var templateEngine = new WorkflowTemplateEngine(_templateStore, graphEngine);
        var versioningEngine = new WorkflowVersioningEngine(_versionStore);

        _engine = new WorkflowValidationOrchestrator(
            definitionEngine,
            graphEngine,
            templateEngine,
            versioningEngine);
    }

    private static IReadOnlyList<WorkflowStep> LinearSteps() => new List<WorkflowStep>
    {
        new("step-1", "Request", "RideEngine", new List<string> { "step-2" }),
        new("step-2", "Match", "DriverMatchEngine", new List<string> { "step-3" }),
        new("step-3", "Complete", "PaymentEngine", new List<string>())
    };

    // ── 1. Valid workflow passes validation ──

    [Fact]
    public void ValidateWorkflowDefinition_ValidWorkflow_ShouldPass()
    {
        var workflow = new WfDefinition("wf-1", "Ride Request", "Test", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);

        var result = _engine.ValidateWorkflowDefinition(workflow);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("Valid", result.ValidationStatus);
        Assert.Equal("wf-1", result.WorkflowId);
    }

    // ── 2. Invalid graph fails validation ──

    [Fact]
    public void ValidateWorkflowDefinition_OrphanNode_ShouldFail()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-2" }),
            new("step-2", "Middle", "EngineB", new List<string>()),
            new("step-3", "Orphan", "EngineC", new List<string>())
        };
        var workflow = new WfDefinition("wf-1", "Bad Graph", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var result = _engine.ValidateWorkflowDefinition(workflow);

        Assert.False(result.IsValid);
        Assert.Equal("Invalid", result.ValidationStatus);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_GRAPH");
    }

    // ── 3. Missing step reference detection ──

    [Fact]
    public void ValidateWorkflowDefinition_MissingStepReference_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-99" }),
            new("step-2", "End", "EngineB", new List<string>())
        };
        var workflow = new WfDefinition("wf-1", "Bad Ref", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var result = _engine.ValidateWorkflowDefinition(workflow);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "MISSING_STEP_REFERENCE");
    }

    // ── 4. Circular dependency detection ──

    [Fact]
    public void ValidateWorkflowDefinition_CircularDependency_ShouldDetect()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-2" }),
            new("step-2", "Second", "EngineB", new List<string> { "step-3" }),
            new("step-3", "Third", "EngineC", new List<string> { "step-1" })
        };
        var workflow = new WfDefinition("wf-1", "Circular", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var result = _engine.ValidateWorkflowDefinition(workflow);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "CIRCULAR_DEPENDENCY");
    }

    // ── 5. Invalid template parameters ──

    [Fact]
    public void ValidateWorkflowTemplate_MissingParameters_ShouldDetect()
    {
        var templateSteps = new List<WfTemplateStep>
        {
            new("step-1", "Process ${entity}", "${engine}", "Execute", new Dictionary<string, string>(), null)
        };
        var transitions = new Dictionary<string, IReadOnlyList<string>>
        {
            ["step-1"] = new List<string>()
        };
        var graph = new WfGraph("tpl-1", transitions);
        var template = new WfTemplate("tpl-1", "Template ${name}", 1, "Desc", templateSteps, graph);
        _templateStore.Register(template);

        var result = _engine.ValidateWorkflowTemplate("tpl-1", new Dictionary<string, string>
        {
            ["entity"] = "Ride"
            // Missing: name, engine
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_TEMPLATE_PARAMETER");
    }

    // ── 6. Invalid version format ──

    [Fact]
    public void ValidateWorkflowVersion_InvalidFormat_ShouldDetect()
    {
        var result = _engine.ValidateWorkflowVersion("wf-1", "invalid-version");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_VERSION");
    }

    [Fact]
    public void ValidateWorkflowVersion_ValidNewVersion_ShouldPass()
    {
        var result = _engine.ValidateWorkflowVersion("wf-1", "1.0.0");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateWorkflowVersion_ExistingVersion_ShouldFail()
    {
        var workflow = new WfDefinition("wf-1", "Test", "Desc", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);
        _versionStore.Store(workflow);

        var result = _engine.ValidateWorkflowVersion("wf-1", "1.0.0");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INVALID_VERSION" && e.Message.Contains("already exists"));
    }

    // ── 7. Multiple validation errors aggregation ──

    [Fact]
    public void ValidateWorkflowDefinition_MultipleErrors_ShouldAggregateAll()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-2" }),
            new("step-1", "Duplicate", "EngineB", new List<string> { "step-99" }),
            new("step-2", "Second", "EngineC", new List<string>())
        };
        var workflow = new WfDefinition("wf-1", "Multi Error", "Test", "1.0.0", steps, DateTimeOffset.UtcNow);

        var result = _engine.ValidateWorkflowDefinition(workflow);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2, $"Expected at least 2 errors, got {result.Errors.Count}");
    }

    // ── Complete workflow validation ──

    [Fact]
    public void ValidateCompleteWorkflow_ValidWorkflow_ShouldPass()
    {
        var workflow = new WfDefinition("wf-new", "Complete Test", "Desc", "1.0.0", LinearSteps(), DateTimeOffset.UtcNow);

        var result = _engine.ValidateCompleteWorkflow(workflow);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCompleteWorkflow_InvalidDefinitionAndVersion_ShouldCombineErrors()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "First", "EngineA", new List<string> { "step-99" })
        };
        var workflow = new WfDefinition("wf-1", "Bad", "Desc", "not-a-version", steps, DateTimeOffset.UtcNow);

        var result = _engine.ValidateCompleteWorkflow(workflow);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Component == "Definition");
        Assert.Contains(result.Errors, e => e.Component == "Version");
    }

    // ── Template not found ──

    [Fact]
    public void ValidateWorkflowTemplate_NotFound_ShouldFail()
    {
        var result = _engine.ValidateWorkflowTemplate("nonexistent", new Dictionary<string, string>());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "TEMPLATE_NOT_FOUND");
    }

    // ── Validation result model tests ──

    [Fact]
    public void WorkflowValidationResult_Valid_ShouldHaveNoErrors()
    {
        var result = WorkflowValidationResult.Valid();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Equal("Valid", result.ValidationStatus);
    }

    [Fact]
    public void WorkflowValidationResult_Combine_ShouldMergeErrors()
    {
        var result1 = WorkflowValidationResult.Invalid(new[]
        {
            new WorkflowValidationError("CODE_1", "Error 1", "Comp1")
        });
        var result2 = WorkflowValidationResult.Invalid(new[]
        {
            new WorkflowValidationError("CODE_2", "Error 2", "Comp2")
        });

        var combined = WorkflowValidationResult.Combine(result1, result2);

        Assert.False(combined.IsValid);
        Assert.Equal(2, combined.Errors.Count);
    }

    [Fact]
    public void WorkflowValidationResult_ValidatedAt_ShouldBeSet()
    {
        var before = DateTimeOffset.UtcNow;
        var result = WorkflowValidationResult.Valid("wf-test");
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(result.ValidatedAt, before, after);
        Assert.Equal("wf-test", result.WorkflowId);
    }
}

// NOTE: WorkflowValidationEngineIEngineTests removed — the WorkflowValidationEngine
// IEngine class was deleted during the T1M refactor. Static validation methods
// (ValidateRetryPolicies, ValidateTimeouts, ValidateParameters, ValidateCommand)
// and the ExecuteAsync IEngine contract need to be re-implemented before these
// tests can be restored.
