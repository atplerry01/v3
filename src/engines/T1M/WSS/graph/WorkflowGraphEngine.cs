namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.System.Midstream.WSS.Models;

public sealed class WorkflowGraphEngine : IWorkflowGraphEngine
{
    public WorkflowGraph BuildGraph(IEnumerable<WorkflowStepDefinition> steps)
    {
        var transitions = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var step in steps)
        {
            transitions[step.StepId] = step.NextSteps;
        }

        return new WorkflowGraph(string.Empty, transitions);
    }

    public IReadOnlyList<string> ValidateGraph(WorkflowGraph graph)
    {
        var violations = new List<string>();

        if (graph.Transitions.Count == 0)
        {
            violations.Add("Graph has no steps.");
            return violations;
        }

        var allNodes = new HashSet<string>(graph.Transitions.Keys);

        // Check for missing nodes (referenced but not defined)
        foreach (var (stepId, nextSteps) in graph.Transitions)
        {
            foreach (var next in nextSteps)
            {
                if (!allNodes.Contains(next))
                    violations.Add($"Step '{stepId}' references undefined node '{next}'.");
            }
        }

        // Identify start steps (no incoming edges)
        var startSteps = GetStartSteps(graph);

        if (startSteps.Count == 0)
        {
            violations.Add("Graph has no start step (all nodes have incoming edges).");
        }
        else
        {
            // BFS from the first start step to find all reachable nodes
            var reachable = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(startSteps[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!reachable.Add(current)) continue;

                if (graph.Transitions.TryGetValue(current, out var nextSteps))
                {
                    foreach (var next in nextSteps)
                    {
                        if (!reachable.Contains(next) && allNodes.Contains(next))
                            queue.Enqueue(next);
                    }
                }
            }

            var orphans = allNodes.Except(reachable).ToList();
            foreach (var orphan in orphans)
                violations.Add($"Orphan node detected: '{orphan}' is unreachable from any start step.");
        }

        // Circular dependency detection using DFS
        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();

        foreach (var node in allNodes)
        {
            if (DetectCycleDfs(node, graph, visited, recStack))
            {
                violations.Add("Circular dependency detected in workflow graph.");
                break;
            }
        }

        return violations;
    }

    public IReadOnlyList<string> GetNextSteps(WorkflowGraph graph, string currentStep)
    {
        if (!graph.Transitions.TryGetValue(currentStep, out var nextSteps))
            throw new KeyNotFoundException($"Step '{currentStep}' not found in graph.");

        return nextSteps;
    }

    public IReadOnlyList<string> GetStartSteps(WorkflowGraph graph)
    {
        var allTargets = new HashSet<string>();

        foreach (var nextSteps in graph.Transitions.Values)
        {
            foreach (var next in nextSteps)
                allTargets.Add(next);
        }

        return graph.Transitions.Keys
            .Where(k => !allTargets.Contains(k))
            .ToList();
    }

    private static bool DetectCycleDfs(
        string node,
        WorkflowGraph graph,
        HashSet<string> visited,
        HashSet<string> recStack)
    {
        if (recStack.Contains(node))
            return true;

        if (visited.Contains(node))
            return false;

        visited.Add(node);
        recStack.Add(node);

        if (graph.Transitions.TryGetValue(node, out var nextSteps))
        {
            foreach (var next in nextSteps)
            {
                if (graph.Transitions.ContainsKey(next) && DetectCycleDfs(next, graph, visited, recStack))
                    return true;
            }
        }

        recStack.Remove(node);
        return false;
    }
}
