namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkflowValidation", EngineTier.T1M, EngineKind.Validation, "WorkflowValidationRequest", typeof(EngineEvent))]
public sealed class WorkflowValidationEngine : IEngine
{
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

        var definitionViolations = ValidateDefinition(graph);
        var graphViolations = ValidateGraph(graph);

        var allViolations = definitionViolations.Concat(graphViolations).ToList();

        if (allViolations.Count > 0)
        {
            var output = new Dictionary<string, object>
            {
                ["violations"] = allViolations,
                ["violationCount"] = allViolations.Count,
                ["workflowId"] = workflowId
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
            ["isValid"] = true
        }));
    }

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

        // Single start node: exactly one step with no incoming edges
        var startNodes = graph.Steps
            .Where(s => incomingEdges[s.StepId].Count == 0)
            .ToList();

        if (startNodes.Count == 0)
            violations.Add("Workflow has no start node (all steps have incoming edges — possible cycle).");
        else if (startNodes.Count > 1)
            violations.Add($"Workflow must have a single start node, found {startNodes.Count}: {string.Join(", ", startNodes.Select(s => s.StepId))}.");

        // No unreachable nodes: BFS from start node(s)
        if (startNodes.Count >= 1)
        {
            var reachable = new HashSet<string>();
            var queue = new Queue<string>(startNodes.Select(s => s.StepId));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!reachable.Add(current)) continue;

                var step = graph.Steps.FirstOrDefault(s => s.StepId == current);
                if (step is null) continue;

                foreach (var next in step.NextSteps)
                {
                    if (!reachable.Contains(next))
                        queue.Enqueue(next);
                }
            }

            var unreachable = stepIds.Except(reachable).ToList();
            if (unreachable.Count > 0)
                violations.Add($"Unreachable steps detected: {string.Join(", ", unreachable)}.");
        }

        return violations;
    }
}
