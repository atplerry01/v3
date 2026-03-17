namespace Whycespace.Engines.T1M.WSS.Definition;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Workflows;
using SystemWorkflowDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;
using DomainWorkflowStepDefinition = Whycespace.Engines.T1M.WSS.Workflows.WorkflowStepDefinition;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

/// <summary>
/// Creates and validates immutable workflow definitions for the WSS orchestration system.
/// Produces workflow specifications (blueprints) used by the workflow runtime.
/// This engine does NOT execute workflows — it only defines workflow structure.
/// </summary>
[EngineManifest(
    "WorkflowDefinition",
    EngineTier.T1M,
    EngineKind.Mutation,
    "WorkflowDefinitionCommand",
    typeof(EngineEvent))]
public sealed class WorkflowDefinitionEngine : IEngine
{
    private readonly Whycespace.Engines.T1M.WSS.Stores.WorkflowDefinitionStore? _store;

    public WorkflowDefinitionEngine() { }

    public WorkflowDefinitionEngine(Whycespace.Engines.T1M.WSS.Stores.WorkflowDefinitionStore store)
    {
        _store = store;
    }

    public string Name => "WorkflowDefinition";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = WorkflowDefinitionCommand.FromContextData(context.Data);

        // 1. Validate workflow name
        if (string.IsNullOrWhiteSpace(command.WorkflowName))
            return Task.FromResult(EngineResult.Fail("WorkflowName must not be empty."));

        // 2. Validate workflow version
        if (string.IsNullOrWhiteSpace(command.WorkflowVersion))
            return Task.FromResult(EngineResult.Fail("WorkflowVersion must not be empty."));

        // 3. Validate step list is non-empty
        if (command.WorkflowSteps.Count == 0)
            return Task.FromResult(EngineResult.Fail("Workflow must have at least one step."));

        // 4. Validate individual steps
        var stepValidationError = ValidateSteps(command.WorkflowSteps);
        if (stepValidationError is not null)
            return Task.FromResult(EngineResult.Fail(stepValidationError));

        // 5. Validate step dependency graph (references + cycles)
        var dependencyError = ValidateDependencyGraph(command.WorkflowSteps);
        if (dependencyError is not null)
            return Task.FromResult(EngineResult.Fail(dependencyError));

        // 6. Validate engine mappings (non-empty engine names)
        var engineMappingError = ValidateEngineMappings(command.WorkflowSteps);
        if (engineMappingError is not null)
            return Task.FromResult(EngineResult.Fail(engineMappingError));

        // 7. Generate deterministic WorkflowId
        var workflowId = GenerateDeterministicWorkflowId(
            command.WorkflowName,
            command.WorkflowVersion,
            command.WorkflowSteps);

        // 8. Map steps to domain models
        var domainSteps = command.WorkflowSteps.Select(s => new DomainWorkflowStepDefinition(
            s.StepId,
            s.StepName,
            s.EngineName,
            s.Dependencies,
            s.RetryPolicy is not null
                ? new WorkflowRetryPolicy(s.RetryPolicy.MaxRetries, s.RetryPolicy.RetryDelay, null)
                : null,
            new WorkflowTimeout(s.Timeout)
        )).ToList();

        var domainParameters = command.WorkflowParameters.Select(p => new WorkflowParameterDefinition(
            p.ParameterName,
            p.ParameterType,
            p.Required,
            null
        )).ToList();

        var createdAt = command.Timestamp;

        // 9. Produce events
        var aggregateId = Guid.TryParse(workflowId, out var parsed) ? parsed : Guid.Empty;

        var events = new[]
        {
            EngineEvent.Create("WorkflowDefinitionCreated", aggregateId,
                new Dictionary<string, object>
                {
                    ["workflowId"] = workflowId,
                    ["workflowName"] = command.WorkflowName,
                    ["workflowVersion"] = command.WorkflowVersion,
                    ["stepCount"] = domainSteps.Count,
                    ["parameterCount"] = domainParameters.Count,
                    ["requestedBy"] = command.RequestedBy,
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.wss.workflow.events"
                })
        };

        // 10. Return result with full definition data
        var output = new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["workflowName"] = command.WorkflowName,
            ["workflowVersion"] = command.WorkflowVersion,
            ["stepCount"] = domainSteps.Count,
            ["parameterCount"] = domainParameters.Count,
            ["createdAt"] = createdAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static string? ValidateSteps(IReadOnlyList<WorkflowStepInput> steps)
    {
        var seenIds = new HashSet<string>();

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.StepId))
                return "Every step must have a non-empty StepId.";

            if (string.IsNullOrWhiteSpace(step.StepName))
                return $"Step '{step.StepId}': StepName must not be empty.";

            if (!seenIds.Add(step.StepId))
                return $"Duplicate step ID: '{step.StepId}'.";
        }

        return null;
    }

    private static string? ValidateDependencyGraph(IReadOnlyList<WorkflowStepInput> steps)
    {
        var validIds = new HashSet<string>(steps.Select(s => s.StepId));

        // Validate all dependency references point to existing steps
        foreach (var step in steps)
        {
            foreach (var dep in step.Dependencies)
            {
                if (!validIds.Contains(dep))
                    return $"Step '{step.StepId}': dependency '{dep}' does not exist.";
            }
        }

        // Detect circular dependencies via topological sort (Kahn's algorithm)
        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var step in steps)
        {
            inDegree[step.StepId] = 0;
            adjacency[step.StepId] = new List<string>();
        }

        // Dependencies mean: dep must complete before this step
        // So edge direction is: dep -> step (dep is a prerequisite)
        foreach (var step in steps)
        {
            foreach (var dep in step.Dependencies)
            {
                adjacency[dep].Add(step.StepId);
                inDegree[step.StepId]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted++;

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (sorted < steps.Count)
            return "Circular dependency detected in workflow steps.";

        return null;
    }

    private static string? ValidateEngineMappings(IReadOnlyList<WorkflowStepInput> steps)
    {
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.EngineName))
                return $"Step '{step.StepId}': EngineName must not be empty.";
        }

        return null;
    }

    public IReadOnlyList<string> ValidateWorkflowDefinition(SystemWorkflowDefinition definition)
    {
        var violations = new List<string>();

        if (string.IsNullOrWhiteSpace(definition.WorkflowId))
            violations.Add("WorkflowId must not be empty.");

        if (string.IsNullOrWhiteSpace(definition.Name))
            violations.Add("Workflow name must not be empty.");

        if (definition.Steps.Count == 0)
        {
            violations.Add("Workflow must have at least one step.");
            return violations;
        }

        // Unique step IDs
        var duplicates = definition.Steps
            .GroupBy(s => s.StepId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var dup in duplicates)
            violations.Add($"Duplicate step ID: '{dup}'.");

        // Graph references must point to existing steps
        var validIds = new HashSet<string>(definition.Steps.Select(s => s.StepId));
        foreach (var step in definition.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!validIds.Contains(next))
                    violations.Add($"Step '{step.StepId}': NextStep '{next}' does not exist.");
            }
        }

        // Circular dependency detection via topological sort (Kahn's algorithm)
        var circularViolation = DetectCircularDependencyInWorkflowSteps(definition.Steps);
        if (circularViolation is not null)
            violations.Add(circularViolation);

        return violations;
    }

    private static string? DetectCircularDependencyInWorkflowSteps(IReadOnlyList<WorkflowStep> steps)
    {
        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var step in steps)
        {
            inDegree.TryAdd(step.StepId, 0);
            adjacency.TryAdd(step.StepId, new List<string>());
        }

        foreach (var step in steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!inDegree.ContainsKey(next)) continue;
                adjacency[step.StepId].Add(next);
                inDegree[next]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted++;

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (sorted < steps.Count)
            return "Circular dependency detected in workflow steps.";

        return null;
    }

    public static string GenerateDeterministicWorkflowId(
        string workflowName,
        string workflowVersion,
        IReadOnlyList<WorkflowStepInput> steps)
    {
        var sb = new StringBuilder();
        sb.Append(workflowName);
        sb.Append('|');
        sb.Append(workflowVersion);
        sb.Append('|');

        // Include step structure in deterministic order
        foreach (var step in steps.OrderBy(s => s.StepId, StringComparer.Ordinal))
        {
            sb.Append(step.StepId);
            sb.Append(':');
            sb.Append(step.EngineName);
            sb.Append(':');
            sb.Append(string.Join(",", step.Dependencies.OrderBy(d => d, StringComparer.Ordinal)));
            sb.Append(';');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    public IReadOnlyCollection<SystemWorkflowDefinition> ListWorkflowDefinitions()
    {
        if (_store is null)
            throw new InvalidOperationException("WorkflowDefinitionStore is not configured.");
        return _store.GetAll();
    }

    public SystemWorkflowDefinition GetWorkflowDefinition(string workflowId)
    {
        if (_store is null)
            throw new InvalidOperationException("WorkflowDefinitionStore is not configured.");
        return _store.Get(workflowId);
    }

    public SystemWorkflowDefinition RegisterWorkflowDefinition(
        string workflowId,
        string name,
        string description,
        string version,
        IReadOnlyList<Whycespace.Contracts.Workflows.WorkflowStep> steps)
    {
        if (_store is null)
            throw new InvalidOperationException("WorkflowDefinitionStore is not configured.");
        var definition = new SystemWorkflowDefinition(workflowId, name, description, version, steps, DateTimeOffset.UtcNow);
        _store.Register(definition);
        return definition;
    }
}
