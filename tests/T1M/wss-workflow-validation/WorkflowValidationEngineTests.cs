using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Engines.T1M.WSS.Validation;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;
using WfGraph = Whycespace.Systems.Midstream.WSS.Models.WorkflowGraph;
using WfTemplate = Whycespace.Systems.Midstream.WSS.Models.WorkflowTemplate;
using WfTemplateStep = Whycespace.Systems.Midstream.WSS.Models.WorkflowTemplateStep;

namespace Whycespace.WSS.WorkflowValidation.Tests;

public class WorkflowValidationEngineTests
{
    private readonly WorkflowValidationOrchestrator _engine;
    private readonly WorkflowTemplateStore _templateStore;
    private readonly WorkflowVersionStore _versionStore;

    public WorkflowValidationEngineTests()
    {
        var definitionStore = new WorkflowDefinitionStore();
        _templateStore = new WorkflowTemplateStore();
        _versionStore = new WorkflowVersionStore();

        var graphEngine = new WorkflowGraphEngine();
        var definitionEngine = new WorkflowDefinitionEngine();
        var templateEngine = new WorkflowTemplateEngine(_templateStore, graphEngine);
        var versioningEngine = new WorkflowVersioningEngine(_versionStore, definitionStore);

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

/// <summary>
/// Tests for the IEngine-based WorkflowValidationEngine (2.1.6).
/// Covers retry policy, timeout, parameter, and DAG validation.
/// </summary>
public class WorkflowValidationEngineIEngineTests
{
    private readonly WorkflowValidationEngine _engine = new();

    // ── Valid workflow via IEngine ──

    [Fact]
    public async Task ExecuteAsync_ValidWorkflow_ShouldSucceed()
    {
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-2" }),
            new("step-2", "End", "EngineB", new List<string>())
        };

        var context = new EngineContext(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "Validate",
            "partition-1",
            new Dictionary<string, object>
            {
                ["workflowId"] = Guid.NewGuid().ToString(),
                ["workflowName"] = "TestWorkflow",
                ["steps"] = (IReadOnlyList<WorkflowStep>)steps
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "WorkflowValidationPassed");
        Assert.Equal("Valid", result.Output["validationStatus"]);
    }

    // ── Missing workflowId ──

    [Fact]
    public async Task ExecuteAsync_MissingWorkflowId_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Validate",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // ── Missing steps ──

    [Fact]
    public async Task ExecuteAsync_NoSteps_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Validate",
            "partition-1", new Dictionary<string, object>
            {
                ["workflowId"] = Guid.NewGuid().ToString(),
                ["workflowName"] = "Empty"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // ── Invalid retry policy ──

    [Fact]
    public void ValidateRetryPolicies_NegativeMaxRetries_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var retryPolicies = new Dictionary<string, WorkflowStepRetryPolicy>
        {
            ["step-1"] = new WorkflowStepRetryPolicy(-1, 5, null)
        };

        var violations = WorkflowValidationEngine.ValidateRetryPolicies(retryPolicies, graph);

        Assert.Contains(violations, v => v.Contains("MaxRetries must be non-negative"));
    }

    [Fact]
    public void ValidateRetryPolicies_ExceedsMaxRetries_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var retryPolicies = new Dictionary<string, WorkflowStepRetryPolicy>
        {
            ["step-1"] = new WorkflowStepRetryPolicy(99, 5, null)
        };

        var violations = WorkflowValidationEngine.ValidateRetryPolicies(retryPolicies, graph);

        Assert.Contains(violations, v => v.Contains("must not exceed 10"));
    }

    [Fact]
    public void ValidateRetryPolicies_NegativeDelay_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var retryPolicies = new Dictionary<string, WorkflowStepRetryPolicy>
        {
            ["step-1"] = new WorkflowStepRetryPolicy(3, -10, null)
        };

        var violations = WorkflowValidationEngine.ValidateRetryPolicies(retryPolicies, graph);

        Assert.Contains(violations, v => v.Contains("RetryDelaySeconds must be non-negative"));
    }

    [Fact]
    public void ValidateRetryPolicies_InvalidCompensationStep_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var retryPolicies = new Dictionary<string, WorkflowStepRetryPolicy>
        {
            ["step-1"] = new WorkflowStepRetryPolicy(3, 5, "nonexistent-step")
        };

        var violations = WorkflowValidationEngine.ValidateRetryPolicies(retryPolicies, graph);

        Assert.Contains(violations, v => v.Contains("CompensationStepId") && v.Contains("does not exist"));
    }

    [Fact]
    public void ValidateRetryPolicies_UnknownStep_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var retryPolicies = new Dictionary<string, WorkflowStepRetryPolicy>
        {
            ["unknown-step"] = new WorkflowStepRetryPolicy(3, 5, null)
        };

        var violations = WorkflowValidationEngine.ValidateRetryPolicies(retryPolicies, graph);

        Assert.Contains(violations, v => v.Contains("unknown step"));
    }

    [Fact]
    public void ValidateRetryPolicies_ValidPolicy_ShouldPass()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-2" }),
            new("step-2", "End", "EngineB", new List<string>())
        });

        var retryPolicies = new Dictionary<string, WorkflowStepRetryPolicy>
        {
            ["step-1"] = new WorkflowStepRetryPolicy(3, 5, "step-2")
        };

        var violations = WorkflowValidationEngine.ValidateRetryPolicies(retryPolicies, graph);

        Assert.Empty(violations);
    }

    // ── Invalid timeout configuration ──

    [Fact]
    public void ValidateTimeouts_BelowMinimum_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var timeouts = new Dictionary<string, WorkflowStepTimeout>
        {
            ["step-1"] = new WorkflowStepTimeout(0.5)
        };

        var violations = WorkflowValidationEngine.ValidateTimeouts(timeouts, graph);

        Assert.Contains(violations, v => v.Contains("must be at least"));
    }

    [Fact]
    public void ValidateTimeouts_ExceedsMaximum_ShouldDetect()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var timeouts = new Dictionary<string, WorkflowStepTimeout>
        {
            ["step-1"] = new WorkflowStepTimeout(100000) // > 86400
        };

        var violations = WorkflowValidationEngine.ValidateTimeouts(timeouts, graph);

        Assert.Contains(violations, v => v.Contains("must not exceed"));
    }

    [Fact]
    public void ValidateTimeouts_ValidTimeout_ShouldPass()
    {
        var graph = new WorkflowGraph("wf-1", "Test", new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string>())
        });

        var timeouts = new Dictionary<string, WorkflowStepTimeout>
        {
            ["step-1"] = new WorkflowStepTimeout(30)
        };

        var violations = WorkflowValidationEngine.ValidateTimeouts(timeouts, graph);

        Assert.Empty(violations);
    }

    // ── Parameter validation ──

    [Fact]
    public void ValidateParameters_DuplicateName_ShouldDetect()
    {
        var parameters = new List<WorkflowValidationParameter>
        {
            new("param1", "string", true, null),
            new("param1", "int", false, "0")
        };

        var violations = WorkflowValidationEngine.ValidateParameters(parameters);

        Assert.Contains(violations, v => v.Contains("Duplicate parameter name"));
    }

    [Fact]
    public void ValidateParameters_EmptyName_ShouldDetect()
    {
        var parameters = new List<WorkflowValidationParameter>
        {
            new("", "string", true, null)
        };

        var violations = WorkflowValidationEngine.ValidateParameters(parameters);

        Assert.Contains(violations, v => v.Contains("name must not be empty"));
    }

    [Fact]
    public void ValidateParameters_InvalidType_ShouldDetect()
    {
        var parameters = new List<WorkflowValidationParameter>
        {
            new("param1", "invalid_type", true, null)
        };

        var violations = WorkflowValidationEngine.ValidateParameters(parameters);

        Assert.Contains(violations, v => v.Contains("Invalid type"));
    }

    [Fact]
    public void ValidateParameters_ValidParameters_ShouldPass()
    {
        var parameters = new List<WorkflowValidationParameter>
        {
            new("name", "string", true, null),
            new("count", "int", false, "0"),
            new("enabled", "bool", true, "true")
        };

        var violations = WorkflowValidationEngine.ValidateParameters(parameters);

        Assert.Empty(violations);
    }

    // ── Deterministic validation behavior ──

    [Fact]
    public async Task ExecuteAsync_SameInput_ShouldProduceSameResult()
    {
        var workflowId = Guid.NewGuid().ToString();
        var steps = new List<WorkflowStep>
        {
            new("step-1", "Start", "EngineA", new List<string> { "step-2" }),
            new("step-2", "End", "EngineB", new List<string>())
        };

        var context = new EngineContext(
            Guid.NewGuid(), workflowId, "Validate",
            "partition-1", new Dictionary<string, object>
            {
                ["workflowId"] = workflowId,
                ["workflowName"] = "DeterminismTest",
                ["steps"] = (IReadOnlyList<WorkflowStep>)steps
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.Equal(result1.Success, result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    // ── Concurrency safety ──

    [Fact]
    public async Task ExecuteAsync_ConcurrentRequests_ShouldAllSucceed()
    {
        var tasks = new List<Task<EngineResult>>();

        for (var i = 0; i < 10; i++)
        {
            var workflowId = Guid.NewGuid().ToString();
            var steps = new List<WorkflowStep>
            {
                new("step-1", "Start", $"Engine{i}", new List<string> { "step-2" }),
                new("step-2", "End", $"Engine{i}B", new List<string>())
            };

            var context = new EngineContext(
                Guid.NewGuid(), workflowId, "Validate",
                $"partition-{i}", new Dictionary<string, object>
                {
                    ["workflowId"] = workflowId,
                    ["workflowName"] = $"Concurrent-{i}",
                    ["steps"] = (IReadOnlyList<WorkflowStep>)steps
                });

            tasks.Add(_engine.ExecuteAsync(context));
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }

    // ── DAG dependency validation via command ──

    [Fact]
    public void ValidateCommand_CircularDependencies_ShouldDetect()
    {
        var command = new WorkflowValidationCommand(
            Guid.NewGuid().ToString(),
            "CircularTest",
            "1.0.0",
            new List<WorkflowValidationStep>
            {
                new("step-1", "First", "EngineA", new List<string> { "step-3" }, null, null),
                new("step-2", "Second", "EngineB", new List<string> { "step-1" }, null, null),
                new("step-3", "Third", "EngineC", new List<string> { "step-2" }, null, null)
            },
            new List<WorkflowValidationParameter>(),
            new WorkflowValidationGraph(new Dictionary<string, IReadOnlyList<string>>()));

        var result = WorkflowValidationEngine.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Circular dependency"));
    }

    [Fact]
    public void ValidateCommand_ValidCommand_ShouldPass()
    {
        var command = new WorkflowValidationCommand(
            Guid.NewGuid().ToString(),
            "ValidTest",
            "1.0.0",
            new List<WorkflowValidationStep>
            {
                new("step-1", "First", "EngineA", new List<string>(), null, null),
                new("step-2", "Second", "EngineB", new List<string> { "step-1" }, null, null)
            },
            new List<WorkflowValidationParameter>
            {
                new("name", "string", true, null)
            },
            new WorkflowValidationGraph(new Dictionary<string, IReadOnlyList<string>>()));

        var result = WorkflowValidationEngine.ValidateCommand(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCommand_StepRetryAndTimeoutViolations_ShouldDetect()
    {
        var command = new WorkflowValidationCommand(
            Guid.NewGuid().ToString(),
            "BadPolicies",
            "1.0.0",
            new List<WorkflowValidationStep>
            {
                new("step-1", "First", "EngineA", new List<string>(),
                    new WorkflowStepRetryPolicy(-1, 5, null),
                    new WorkflowStepTimeout(0.1)),
                new("step-2", "Second", "EngineB", new List<string> { "step-1" },
                    new WorkflowStepRetryPolicy(3, -5, "nonexistent"),
                    new WorkflowStepTimeout(999999))
            },
            new List<WorkflowValidationParameter>(),
            new WorkflowValidationGraph(new Dictionary<string, IReadOnlyList<string>>()));

        var result = WorkflowValidationEngine.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("MaxRetries must be non-negative"));
        Assert.Contains(result.Errors, e => e.Contains("Timeout must be at least"));
        Assert.Contains(result.Errors, e => e.Contains("RetryDelaySeconds must be non-negative"));
        Assert.Contains(result.Errors, e => e.Contains("Timeout must not exceed"));
        Assert.Contains(result.Errors, e => e.Contains("CompensationStepId") && e.Contains("does not exist"));
    }

    [Fact]
    public void ValidateCommand_MissingDependencyReference_ShouldDetect()
    {
        var command = new WorkflowValidationCommand(
            Guid.NewGuid().ToString(),
            "BadDeps",
            "1.0.0",
            new List<WorkflowValidationStep>
            {
                new("step-1", "First", "EngineA", new List<string> { "nonexistent" }, null, null)
            },
            new List<WorkflowValidationParameter>(),
            new WorkflowValidationGraph(new Dictionary<string, IReadOnlyList<string>>()));

        var result = WorkflowValidationEngine.ValidateCommand(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Dependency") && e.Contains("does not exist"));
    }
}
