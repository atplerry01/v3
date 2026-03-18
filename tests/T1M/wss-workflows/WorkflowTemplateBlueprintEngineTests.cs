using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

namespace Whycespace.WSS.Workflows.Tests;

public class WorkflowTemplateBlueprintEngineTests
{
    private readonly WorkflowTemplateBlueprintEngine _engine = new();

    private static EngineContext CreateContext(IDictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(),
            "wf-test",
            "template-creation",
            new PartitionKey("test-partition"),
            new Dictionary<string, object>(data));
    }

    private static WorkflowTemplateCommand CreateValidCommand(
        string name = "SPV Creation Workflow",
        int version = 1)
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-validate", "Validate Input", "ValidationEngine",
                new List<string>(),
                new Dictionary<string, string> { ["entityId"] = "${PropertyId}" },
                TimeSpan.FromSeconds(30),
                null),
            new("step-create", "Create SPV", "SPVEngine",
                new List<string> { "step-validate" },
                new Dictionary<string, string> { ["investorId"] = "${InvestorId}" },
                TimeSpan.FromSeconds(60),
                new WorkflowFailurePolicy(FailureAction.Retry, 3, TimeSpan.FromSeconds(5), null)),
            new("step-notify", "Send Notification", "NotificationEngine",
                new List<string> { "step-create" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(15),
                null)
        };

        var parameters = new List<WorkflowTemplateParameter>
        {
            new("PropertyId", "string", true),
            new("InvestorId", "string", true),
            new("VaultId", "string", false)
        };

        return new WorkflowTemplateCommand(
            name,
            "Standard SPV creation workflow template",
            version,
            steps,
            parameters,
            "admin@whycespace.com",
            DateTimeOffset.UtcNow);
    }

    private static IDictionary<string, object> CommandToData(WorkflowTemplateCommand cmd)
    {
        return new Dictionary<string, object>
        {
            ["templateName"] = cmd.TemplateName,
            ["templateDescription"] = cmd.TemplateDescription,
            ["templateVersion"] = cmd.TemplateVersion,
            ["templateSteps"] = cmd.TemplateSteps,
            ["templateParameters"] = cmd.TemplateParameters,
            ["requestedBy"] = cmd.RequestedBy,
            ["timestamp"] = cmd.Timestamp
        };
    }

    // 1. Valid template creation
    [Fact]
    public async Task ExecuteAsync_ValidTemplate_ShouldSucceed()
    {
        var command = CreateValidCommand();
        var context = CreateContext(CommandToData(command));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("WorkflowTemplateCreated", result.Events[0].EventType);
        Assert.Equal("SPV Creation Workflow", result.Output["templateName"]);
        Assert.Equal(1, result.Output["templateVersion"]);
    }

    // 2. Deterministic template ID generation
    [Fact]
    public void GenerateTemplateId_SameInput_ShouldProduceSameId()
    {
        var command1 = CreateValidCommand();
        var command2 = CreateValidCommand();

        var id1 = WorkflowTemplateBlueprintEngine.GenerateTemplateId(command1);
        var id2 = WorkflowTemplateBlueprintEngine.GenerateTemplateId(command2);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateTemplateId_DifferentInput_ShouldProduceDifferentId()
    {
        var command1 = CreateValidCommand("Template A", 1);
        var command2 = CreateValidCommand("Template B", 1);

        var id1 = WorkflowTemplateBlueprintEngine.GenerateTemplateId(command1);
        var id2 = WorkflowTemplateBlueprintEngine.GenerateTemplateId(command2);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GenerateTemplateId_DifferentVersion_ShouldProduceDifferentId()
    {
        var command1 = CreateValidCommand("Template A", 1);
        var command2 = CreateValidCommand("Template A", 2);

        var id1 = WorkflowTemplateBlueprintEngine.GenerateTemplateId(command1);
        var id2 = WorkflowTemplateBlueprintEngine.GenerateTemplateId(command2);

        Assert.NotEqual(id1, id2);
    }

    // 3. Dependency validation — invalid reference
    [Fact]
    public void Validate_InvalidDependencyReference_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string> { "step-nonexistent" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Bad Deps", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("non-existent step"));
    }

    // 4. Circular dependency detection
    [Fact]
    public void Validate_CircularDependency_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string> { "step-b" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null),
            new("step-b", "Step B", "EngineB",
                new List<string> { "step-c" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null),
            new("step-c", "Step C", "EngineC",
                new List<string> { "step-a" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Circular", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("Circular dependency"));
    }

    // 5. Missing parameter binding validation
    [Fact]
    public void Validate_UndeclaredParameterBinding_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string>(),
                new Dictionary<string, string> { ["key"] = "${UndeclaredParam}" },
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Bad Bindings", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("undeclared parameter"));
    }

    // 6. Template structure validation — empty steps
    [Fact]
    public void Validate_NoSteps_ShouldReturnError()
    {
        var command = new WorkflowTemplateCommand(
            "Empty", "Desc", 1,
            new List<WorkflowTemplateCommandStep>(),
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("at least one step"));
    }

    // 7. Template name validation
    [Fact]
    public void Validate_EmptyName_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("TemplateName is required"));
    }

    // 8. Invalid version
    [Fact]
    public void Validate_ZeroVersion_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Test", "Desc", 0, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("TemplateVersion must be >= 1"));
    }

    // 9. Duplicate step IDs
    [Fact]
    public void Validate_DuplicateStepIds_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null),
            new("step-a", "Step A Dup", "EngineB",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Dup Steps", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("Duplicate step ID"));
    }

    // 10. Self-dependency detection
    [Fact]
    public void Validate_SelfDependency_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string> { "step-a" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Self Dep", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("cannot depend on itself"));
    }

    // 11. Missing engine reference
    [Fact]
    public void Validate_MissingEngineName_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "No Engine", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("must reference an engine"));
    }

    // 12. Invalid timeout
    [Fact]
    public void Validate_ZeroTimeout_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.Zero, null)
        };

        var command = new WorkflowTemplateCommand(
            "Bad Timeout", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("positive timeout"));
    }

    // 13. Missing context data should fail
    [Fact]
    public async Task ExecuteAsync_MissingData_ShouldFail()
    {
        var context = CreateContext(new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Contains("Invalid or missing", result.Output["error"] as string);
    }

    // 14. Concurrent command execution (thread-safety)
    [Fact]
    public async Task ExecuteAsync_ConcurrentCalls_ShouldAllSucceed()
    {
        var tasks = Enumerable.Range(1, 10).Select(i =>
        {
            var command = CreateValidCommand($"Template {i}", i);
            var context = CreateContext(CommandToData(command));
            return _engine.ExecuteAsync(context);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        var templateIds = results.Select(r => r.Output["templateId"] as string).ToList();
        Assert.Equal(templateIds.Distinct().Count(), templateIds.Count);
    }

    // 15. Parameter type validation
    [Fact]
    public void Validate_ParameterMissingType_ShouldReturnError()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-a", "Step A", "EngineA",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var parameters = new List<WorkflowTemplateParameter>
        {
            new("PropertyId", "", true)
        };

        var command = new WorkflowTemplateCommand(
            "Bad Param", "Desc", 1, steps, parameters,
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Contains(errors, e => e.Contains("must have a type"));
    }

    // 16. Valid template with complex dependency graph (diamond pattern)
    [Fact]
    public void Validate_DiamondDependency_ShouldPass()
    {
        var steps = new List<WorkflowTemplateCommandStep>
        {
            new("step-start", "Start", "EngineA",
                new List<string>(),
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null),
            new("step-left", "Left Branch", "EngineB",
                new List<string> { "step-start" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null),
            new("step-right", "Right Branch", "EngineC",
                new List<string> { "step-start" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null),
            new("step-join", "Join", "EngineD",
                new List<string> { "step-left", "step-right" },
                new Dictionary<string, string>(),
                TimeSpan.FromSeconds(30), null)
        };

        var command = new WorkflowTemplateCommand(
            "Diamond", "Desc", 1, steps,
            new List<WorkflowTemplateParameter>(),
            "admin", DateTimeOffset.UtcNow);

        var errors = WorkflowTemplateBlueprintEngine.Validate(command);

        Assert.Empty(errors);
    }

    // 17. Output contains all required fields
    [Fact]
    public async Task ExecuteAsync_ValidTemplate_ShouldReturnAllOutputFields()
    {
        var command = CreateValidCommand();
        var context = CreateContext(CommandToData(command));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("templateId"));
        Assert.True(result.Output.ContainsKey("templateName"));
        Assert.True(result.Output.ContainsKey("templateVersion"));
        Assert.True(result.Output.ContainsKey("templateSteps"));
        Assert.True(result.Output.ContainsKey("templateParameters"));
        Assert.True(result.Output.ContainsKey("createdAt"));
    }

    // 18. Event payload contains expected data
    [Fact]
    public async Task ExecuteAsync_ValidTemplate_ShouldEmitCorrectEventPayload()
    {
        var command = CreateValidCommand();
        var context = CreateContext(CommandToData(command));

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("WorkflowTemplateCreated", evt.EventType);
        Assert.Equal("SPV Creation Workflow", evt.Payload["templateName"]);
        Assert.Equal(1, evt.Payload["templateVersion"]);
        Assert.Equal(3, evt.Payload["stepCount"]);
        Assert.Equal(3, evt.Payload["parameterCount"]);
        Assert.Equal("admin@whycespace.com", evt.Payload["requestedBy"]);
    }
}
