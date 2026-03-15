namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Validation;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

/// <summary>
/// Workflow Validation Engine (2.1.6) — T1M Orchestration Engine.
/// Validates workflow definitions, graphs, and templates before acceptance into WSS.
/// Stateless, deterministic, thread-safe.
/// </summary>
[EngineManifest("WorkflowValidation", EngineTier.T1M, EngineKind.Validation, "WorkflowValidationCommand", typeof(EngineEvent))]
public sealed class WorkflowValidationEngine : IEngine
{
    internal const int MaxRetryPolicyRetries = 10;
    internal const double MaxRetryDelaySeconds = 1800; // 30 minutes
    internal const double MinTimeoutSeconds = 1;
    internal const double MaxTimeoutSeconds = 86400; // 24 hours

    public string Name => "WorkflowValidation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string ?? "Unknown";

        if (string.IsNullOrWhiteSpace(workflowId))
            return Task.FromResult(EngineResult.Fail("Missing workflowId"));

        var steps = context.Data.GetValueOrDefault("steps") as IReadOnlyList<WorkflowStep>;
        if (steps is null || steps.Count == 0)
            return Task.FromResult(EngineResult.Fail("Workflow must have at least one step"));

        var graph = new WorkflowGraph(workflowId, workflowName, steps);

        var allViolations = new List<string>();
        allViolations.AddRange(ValidateDefinition(graph));
        allViolations.AddRange(ValidateGraph(graph));

        // Validate retry policies if provided
        var retryPolicies = context.Data.GetValueOrDefault("retryPolicies")
            as IReadOnlyDictionary<string, WorkflowStepRetryPolicy>;
        if (retryPolicies is not null)
            allViolations.AddRange(ValidateRetryPolicies(retryPolicies, graph));

        // Validate timeout configurations if provided
        var timeouts = context.Data.GetValueOrDefault("timeouts")
            as IReadOnlyDictionary<string, WorkflowStepTimeout>;
        if (timeouts is not null)
            allViolations.AddRange(ValidateTimeouts(timeouts, graph));

        // Validate parameters if provided
        var parameters = context.Data.GetValueOrDefault("parameters")
            as IReadOnlyList<WorkflowValidationParameter>;
        if (parameters is not null)
            allViolations.AddRange(ValidateParameters(parameters));

        if (allViolations.Count > 0)
        {
            var output = new Dictionary<string, object>
            {
                ["violations"] = allViolations,
                ["violationCount"] = allViolations.Count,
                ["workflowId"] = workflowId,
                ["validationStatus"] = "Invalid",
                ["validatedAt"] = DateTimeOffset.UtcNow.ToString("O")
            };

            var events = new[]
            {
                EngineEvent.Create("WorkflowValidationFailed", Guid.Parse(workflowId),
                    new Dictionary<string, object>
                    {
                        ["workflowName"] = workflowName,
                        ["violationCount"] = allViolations.Count
                    })
            };

            return Task.FromResult(new EngineResult(false, events, output));
        }

        var successEvents = new[]
        {
            EngineEvent.Create("WorkflowValidationPassed", Guid.Parse(workflowId),
                new Dictionary<string, object> { ["workflowName"] = workflowName })
        };

        return Task.FromResult(EngineResult.Ok(successEvents, new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["isValid"] = true,
            ["validationStatus"] = "Valid",
            ["validatedAt"] = DateTimeOffset.UtcNow.ToString("O")
        }));
    }

    /// <summary>
    /// Validates workflow metadata and step definitions.
    /// </summary>
    internal static IReadOnlyList<string> ValidateDefinition(WorkflowGraph graph)
    {
        var violations = new List<string>();

        if (string.IsNullOrWhiteSpace(graph.WorkflowId))
            violations.Add("WorkflowId must not be empty.");

        if (string.IsNullOrWhiteSpace(graph.Name))
            violations.Add("Workflow name must not be empty.");

        // All steps must map to an engine
        foreach (var step in graph.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.EngineName))
                violations.Add($"Step '{step.StepId}': must reference an engine.");
        }

        // Step IDs must be unique
        var duplicates = graph.Steps
            .GroupBy(s => s.StepId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var dup in duplicates)
            violations.Add($"Duplicate step ID: '{dup}'.");

        // NextSteps must reference valid step IDs
        var validIds = new HashSet<string>(graph.Steps.Select(s => s.StepId));
        foreach (var step in graph.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!validIds.Contains(next))
                    violations.Add($"Step '{step.StepId}': NextStep '{next}' does not exist.");
            }
        }

        return violations;
    }

    /// <summary>
    /// Validates DAG integrity: single start node, acyclicity, reachability.
    /// </summary>
    internal static IReadOnlyList<string> ValidateGraph(WorkflowGraph graph)
    {
        var violations = new List<string>();
        if (graph.Steps.Count == 0) return violations;

        var stepIds = new HashSet<string>(graph.Steps.Select(s => s.StepId));

        // Build incoming edges map
        var incomingEdges = new Dictionary<string, HashSet<string>>();
        foreach (var step in graph.Steps)
            incomingEdges[step.StepId] = new HashSet<string>();

        foreach (var step in graph.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (incomingEdges.ContainsKey(next))
                    incomingEdges[next].Add(step.StepId);
            }
        }

        // Single start node
        var startNodes = graph.Steps
            .Where(s => incomingEdges[s.StepId].Count == 0)
            .ToList();

        if (startNodes.Count == 0)
            violations.Add("Workflow has no start node (all steps have incoming edges — possible cycle).");
        else if (startNodes.Count > 1)
            violations.Add($"Workflow must have a single start node, found {startNodes.Count}: {string.Join(", ", startNodes.Select(s => s.StepId))}.");

        // Cycle detection + reachability
        if (startNodes.Count >= 1)
        {
            // Kahn's algorithm for cycle detection
            var inDegree = new Dictionary<string, int>();
            foreach (var step in graph.Steps)
                inDegree[step.StepId] = incomingEdges[step.StepId].Count;

            var topoQueue = new Queue<string>(startNodes.Select(s => s.StepId));
            var sorted = new List<string>();

            while (topoQueue.Count > 0)
            {
                var current = topoQueue.Dequeue();
                sorted.Add(current);

                var step = graph.Steps.FirstOrDefault(s => s.StepId == current);
                if (step is null) continue;

                foreach (var next in step.NextSteps)
                {
                    if (!inDegree.ContainsKey(next)) continue;
                    inDegree[next]--;
                    if (inDegree[next] == 0)
                        topoQueue.Enqueue(next);
                }
            }

            if (sorted.Count != graph.Steps.Count)
            {
                var cycleNodes = stepIds.Except(sorted).ToList();
                violations.Add($"Circular dependency detected involving steps: {string.Join(", ", cycleNodes)}.");
            }

            // Reachability via BFS
            var reachable = new HashSet<string>();
            var bfsQueue = new Queue<string>(startNodes.Select(s => s.StepId));

            while (bfsQueue.Count > 0)
            {
                var current = bfsQueue.Dequeue();
                if (!reachable.Add(current)) continue;

                var step = graph.Steps.FirstOrDefault(s => s.StepId == current);
                if (step is null) continue;

                foreach (var next in step.NextSteps)
                {
                    if (!reachable.Contains(next))
                        bfsQueue.Enqueue(next);
                }
            }

            var unreachable = stepIds.Except(reachable).ToList();
            if (unreachable.Count > 0)
                violations.Add($"Unreachable steps detected: {string.Join(", ", unreachable)}.");
        }

        return violations;
    }

    /// <summary>
    /// Validates retry policies for workflow steps.
    /// </summary>
    internal static IReadOnlyList<string> ValidateRetryPolicies(
        IReadOnlyDictionary<string, WorkflowStepRetryPolicy> retryPolicies,
        WorkflowGraph graph)
    {
        var violations = new List<string>();
        var validStepIds = new HashSet<string>(graph.Steps.Select(s => s.StepId));

        foreach (var (stepId, policy) in retryPolicies)
        {
            if (!validStepIds.Contains(stepId))
            {
                violations.Add($"RetryPolicy references unknown step '{stepId}'.");
                continue;
            }

            if (policy.MaxRetries < 0)
                violations.Add($"Step '{stepId}': MaxRetries must be non-negative, got {policy.MaxRetries}.");

            if (policy.MaxRetries > MaxRetryPolicyRetries)
                violations.Add($"Step '{stepId}': MaxRetries must not exceed {MaxRetryPolicyRetries}, got {policy.MaxRetries}.");

            if (policy.RetryDelaySeconds < 0)
                violations.Add($"Step '{stepId}': RetryDelaySeconds must be non-negative, got {policy.RetryDelaySeconds}.");

            if (policy.RetryDelaySeconds > MaxRetryDelaySeconds)
                violations.Add($"Step '{stepId}': RetryDelaySeconds must not exceed {MaxRetryDelaySeconds}, got {policy.RetryDelaySeconds}.");

            if (policy.CompensationStepId is not null && !validStepIds.Contains(policy.CompensationStepId))
                violations.Add($"Step '{stepId}': CompensationStepId '{policy.CompensationStepId}' does not exist.");
        }

        return violations;
    }

    /// <summary>
    /// Validates timeout configurations for workflow steps.
    /// </summary>
    internal static IReadOnlyList<string> ValidateTimeouts(
        IReadOnlyDictionary<string, WorkflowStepTimeout> timeouts,
        WorkflowGraph graph)
    {
        var violations = new List<string>();
        var validStepIds = new HashSet<string>(graph.Steps.Select(s => s.StepId));

        foreach (var (stepId, timeout) in timeouts)
        {
            if (!validStepIds.Contains(stepId))
            {
                violations.Add($"Timeout references unknown step '{stepId}'.");
                continue;
            }

            if (timeout.TimeoutSeconds < MinTimeoutSeconds)
                violations.Add($"Step '{stepId}': Timeout must be at least {MinTimeoutSeconds}s, got {timeout.TimeoutSeconds}s.");

            if (timeout.TimeoutSeconds > MaxTimeoutSeconds)
                violations.Add($"Step '{stepId}': Timeout must not exceed {MaxTimeoutSeconds}s, got {timeout.TimeoutSeconds}s.");
        }

        return violations;
    }

    /// <summary>
    /// Validates parameter definitions for workflow.
    /// </summary>
    internal static IReadOnlyList<string> ValidateParameters(
        IReadOnlyList<WorkflowValidationParameter> parameters)
    {
        var violations = new List<string>();
        var names = new HashSet<string>();

        foreach (var param in parameters)
        {
            if (string.IsNullOrWhiteSpace(param.Name))
            {
                violations.Add("Parameter name must not be empty.");
                continue;
            }

            if (!names.Add(param.Name))
                violations.Add($"Duplicate parameter name: '{param.Name}'.");

            if (string.IsNullOrWhiteSpace(param.Type))
                violations.Add($"Parameter '{param.Name}': Type must not be empty.");

            var validTypes = new HashSet<string> { "string", "int", "decimal", "bool", "datetime", "guid" };
            if (!string.IsNullOrWhiteSpace(param.Type) && !validTypes.Contains(param.Type.ToLowerInvariant()))
                violations.Add($"Parameter '{param.Name}': Invalid type '{param.Type}'. Valid types: {string.Join(", ", validTypes)}.");
        }

        return violations;
    }

    /// <summary>
    /// Validates a complete WorkflowValidationCommand.
    /// Pure method — no side effects, deterministic.
    /// </summary>
    internal static WorkflowCommandValidationResult ValidateCommand(WorkflowValidationCommand command)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Metadata validation
        if (string.IsNullOrWhiteSpace(command.WorkflowId))
            errors.Add("WorkflowId must not be empty.");
        if (string.IsNullOrWhiteSpace(command.WorkflowName))
            errors.Add("WorkflowName must not be empty.");
        if (string.IsNullOrWhiteSpace(command.WorkflowVersion))
            errors.Add("WorkflowVersion must not be empty.");

        if (command.WorkflowSteps.Count == 0)
        {
            errors.Add("Workflow must have at least one step.");
            return new WorkflowCommandValidationResult(
                command.WorkflowId ?? "",
                errors.Count == 0,
                errors,
                warnings,
                DateTimeOffset.UtcNow);
        }

        // Step validation
        var stepIds = new HashSet<string>();
        foreach (var step in command.WorkflowSteps)
        {
            if (string.IsNullOrWhiteSpace(step.StepId))
                errors.Add("Step ID must not be empty.");
            else if (!stepIds.Add(step.StepId))
                errors.Add($"Duplicate step ID: '{step.StepId}'.");

            if (string.IsNullOrWhiteSpace(step.EngineName))
                errors.Add($"Step '{step.StepId}': EngineName must not be empty.");

            // Dependency references
            foreach (var dep in step.Dependencies)
            {
                if (!command.WorkflowSteps.Any(s => s.StepId == dep))
                    errors.Add($"Step '{step.StepId}': Dependency '{dep}' does not exist.");
            }

            // Retry policy validation
            if (step.RetryPolicy is not null)
            {
                if (step.RetryPolicy.MaxRetries < 0)
                    errors.Add($"Step '{step.StepId}': MaxRetries must be non-negative.");
                if (step.RetryPolicy.MaxRetries > MaxRetryPolicyRetries)
                    errors.Add($"Step '{step.StepId}': MaxRetries must not exceed {MaxRetryPolicyRetries}.");
                if (step.RetryPolicy.RetryDelaySeconds < 0)
                    errors.Add($"Step '{step.StepId}': RetryDelaySeconds must be non-negative.");
                if (step.RetryPolicy.RetryDelaySeconds > MaxRetryDelaySeconds)
                    errors.Add($"Step '{step.StepId}': RetryDelaySeconds must not exceed {MaxRetryDelaySeconds}.");
                if (step.RetryPolicy.CompensationStepId is not null &&
                    !command.WorkflowSteps.Any(s => s.StepId == step.RetryPolicy.CompensationStepId))
                    errors.Add($"Step '{step.StepId}': CompensationStepId '{step.RetryPolicy.CompensationStepId}' does not exist.");
            }

            // Timeout validation
            if (step.Timeout is not null)
            {
                if (step.Timeout.TimeoutSeconds < MinTimeoutSeconds)
                    errors.Add($"Step '{step.StepId}': Timeout must be at least {MinTimeoutSeconds}s.");
                if (step.Timeout.TimeoutSeconds > MaxTimeoutSeconds)
                    errors.Add($"Step '{step.StepId}': Timeout must not exceed {MaxTimeoutSeconds}s.");
            }
        }

        // DAG validation via dependency graph
        errors.AddRange(ValidateDependencyDag(command.WorkflowSteps));

        // Parameter validation
        var paramNames = new HashSet<string>();
        foreach (var param in command.WorkflowParameters)
        {
            if (string.IsNullOrWhiteSpace(param.Name))
                errors.Add("Parameter name must not be empty.");
            else if (!paramNames.Add(param.Name))
                errors.Add($"Duplicate parameter name: '{param.Name}'.");

            if (string.IsNullOrWhiteSpace(param.Type))
                errors.Add($"Parameter '{param.Name}': Type must not be empty.");
        }

        return new WorkflowCommandValidationResult(
            command.WorkflowId ?? "",
            errors.Count == 0,
            errors,
            warnings,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Validates the dependency graph is a DAG (no cycles).
    /// Uses Kahn's algorithm for topological sort.
    /// </summary>
    internal static IReadOnlyList<string> ValidateDependencyDag(
        IReadOnlyList<WorkflowValidationStep> steps)
    {
        var violations = new List<string>();
        var stepIds = new HashSet<string>(steps.Select(s => s.StepId));

        // Build in-degree map
        var inDegree = new Dictionary<string, int>();
        foreach (var step in steps)
            inDegree[step.StepId] = 0;

        foreach (var step in steps)
        {
            foreach (var dep in step.Dependencies)
            {
                if (inDegree.ContainsKey(step.StepId))
                    inDegree[step.StepId]++;
            }
        }

        // Kahn's algorithm
        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            // Find steps that depend on current
            foreach (var step in steps)
            {
                if (step.Dependencies.Contains(current))
                {
                    inDegree[step.StepId]--;
                    if (inDegree[step.StepId] == 0)
                        queue.Enqueue(step.StepId);
                }
            }
        }

        if (sorted.Count != steps.Count)
        {
            var cycleNodes = stepIds.Except(sorted).ToList();
            violations.Add($"Circular dependency detected involving steps: {string.Join(", ", cycleNodes)}.");
        }

        return violations;
    }
}

/// <summary>
/// Result of validating a WorkflowValidationCommand.
/// </summary>
public sealed record WorkflowCommandValidationResult(
    string WorkflowId,
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    DateTimeOffset ValidatedAt
);
