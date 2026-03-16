namespace Whycespace.Engines.T1M.WSS.Dependency;

using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowDependencyAnalyzer : IWorkflowDependencyEngine
{
    private readonly WorkflowDefinitionStore _definitionStore;

    public WorkflowDependencyAnalyzer(WorkflowDefinitionStore definitionStore)
    {
        _definitionStore = definitionStore;
    }

    public WorkflowDependencyResult AnalyzeWorkflowDependencies(WorkflowDefinition workflow)
    {
        var dependencies = BuildDependencyMap(workflow);
        var executionOrder = ResolveExecutionOrder(workflow);
        var missing = DetectMissingDependencies(workflow);
        var circular = DetectCircularDependencies(workflow);
        var external = GetExternalWorkflowDependencies(workflow);

        return new WorkflowDependencyResult(
            workflow.WorkflowId,
            dependencies,
            executionOrder,
            missing,
            circular,
            external);
    }

    public IReadOnlyList<string> ResolveExecutionOrder(WorkflowDefinition workflow)
    {
        if (workflow.Steps.Count == 0)
            return Array.Empty<string>();

        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var step in workflow.Steps)
        {
            inDegree.TryAdd(step.StepId, 0);
            adjacency.TryAdd(step.StepId, new List<string>());
        }

        foreach (var step in workflow.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!inDegree.ContainsKey(next)) continue;
                adjacency[step.StepId].Add(next);
                inDegree[next]++;
            }
        }

        // Kahn's algorithm — deterministic via sorted queue
        var queue = new SortedSet<string>(
            inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key),
            StringComparer.Ordinal);

        var order = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Min!;
            queue.Remove(current);
            order.Add(current);

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Add(neighbor);
            }
        }

        return order;
    }

    public IReadOnlyList<string> DetectCircularDependencies(WorkflowDefinition workflow)
    {
        if (workflow.Steps.Count == 0)
            return Array.Empty<string>();

        var adjacency = new Dictionary<string, List<string>>();
        foreach (var step in workflow.Steps)
            adjacency[step.StepId] = new List<string>();

        foreach (var step in workflow.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (adjacency.ContainsKey(next))
                    adjacency[step.StepId].Add(next);
            }
        }

        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();
        var cyclicSteps = new List<string>();

        foreach (var step in workflow.Steps)
        {
            if (!visited.Contains(step.StepId))
                DetectCycleDfs(step.StepId, adjacency, visited, recStack, cyclicSteps);
        }

        return cyclicSteps;
    }

    public IReadOnlyList<string> GetExternalWorkflowDependencies(WorkflowDefinition workflow)
    {
        var external = new HashSet<string>();
        var knownWorkflows = new HashSet<string>();

        try
        {
            foreach (var wf in _definitionStore.GetAll())
                knownWorkflows.Add(wf.WorkflowId);
        }
        catch
        {
            // Store may be empty, that's fine
        }

        foreach (var step in workflow.Steps)
        {
            // Detect references to other workflows by engine name convention
            // If an engine name matches a known workflow ID, it's an external dependency
            if (knownWorkflows.Contains(step.EngineName) && step.EngineName != workflow.WorkflowId)
            {
                external.Add(step.EngineName);
            }

            // Also detect workflow references in step names containing "Workflow" suffix
            if (step.EngineName.EndsWith("Workflow", StringComparison.OrdinalIgnoreCase)
                && step.EngineName != workflow.WorkflowId)
            {
                external.Add(step.EngineName);
            }
        }

        return external.ToList();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildDependencyMap(WorkflowDefinition workflow)
    {
        // For each step, determine which steps it depends on (incoming edges = dependencies)
        var dependencies = new Dictionary<string, IReadOnlyList<string>>();

        // Build reverse map: for each step, who points to it?
        var incoming = new Dictionary<string, List<string>>();
        foreach (var step in workflow.Steps)
            incoming[step.StepId] = new List<string>();

        foreach (var step in workflow.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (incoming.ContainsKey(next))
                    incoming[next].Add(step.StepId);
            }
        }

        foreach (var (stepId, deps) in incoming)
            dependencies[stepId] = deps;

        return dependencies;
    }

    private static IReadOnlyList<string> DetectMissingDependencies(WorkflowDefinition workflow)
    {
        var validIds = new HashSet<string>(workflow.Steps.Select(s => s.StepId));
        var missing = new List<string>();

        foreach (var step in workflow.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!validIds.Contains(next))
                    missing.Add($"Step '{step.StepId}' references missing step '{next}'");
            }
        }

        return missing;
    }

    private static void DetectCycleDfs(
        string node,
        Dictionary<string, List<string>> adjacency,
        HashSet<string> visited,
        HashSet<string> recStack,
        List<string> cyclicSteps)
    {
        visited.Add(node);
        recStack.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var next in neighbors)
            {
                if (!visited.Contains(next))
                {
                    DetectCycleDfs(next, adjacency, visited, recStack, cyclicSteps);
                }
                else if (recStack.Contains(next))
                {
                    if (!cyclicSteps.Contains(next))
                        cyclicSteps.Add(next);
                    if (!cyclicSteps.Contains(node))
                        cyclicSteps.Add(node);
                }
            }
        }

        recStack.Remove(node);
    }
}
