namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Engines.T1M.Shared;
using WorkflowGraph = Whycespace.Systems.Midstream.WSS.Models.WorkflowGraph;
using WorkflowStepDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowStepDefinition;

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

    public IReadOnlyList<string> ComputeExecutionOrder(WorkflowGraph graph)
    {
        var inDegree = new Dictionary<string, int>();
        foreach (var node in graph.Transitions.Keys)
            inDegree[node] = 0;

        foreach (var (_, nextSteps) in graph.Transitions)
        {
            foreach (var next in nextSteps)
            {
                if (inDegree.ContainsKey(next))
                    inDegree[next]++;
            }
        }

        var queue = new Queue<string>();
        foreach (var (node, degree) in inDegree.OrderBy(kv => kv.Key))
        {
            if (degree == 0)
                queue.Enqueue(node);
        }

        var order = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);

            if (!graph.Transitions.TryGetValue(current, out var nextSteps))
                continue;

            foreach (var next in nextSteps.OrderBy(n => n))
            {
                if (!inDegree.ContainsKey(next))
                    continue;

                inDegree[next]--;
                if (inDegree[next] == 0)
                    queue.Enqueue(next);
            }
        }

        return order;
    }

    public IReadOnlyList<IReadOnlyList<string>> ComputeParallelGroups(WorkflowGraph graph)
    {
        var inDegree = new Dictionary<string, int>();
        foreach (var node in graph.Transitions.Keys)
            inDegree[node] = 0;

        foreach (var (_, nextSteps) in graph.Transitions)
        {
            foreach (var next in nextSteps)
            {
                if (inDegree.ContainsKey(next))
                    inDegree[next]++;
            }
        }

        var depth = new Dictionary<string, int>();
        var queue = new Queue<string>();

        foreach (var (node, degree) in inDegree)
        {
            if (degree == 0)
            {
                depth[node] = 0;
                queue.Enqueue(node);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!graph.Transitions.TryGetValue(current, out var nextSteps))
                continue;

            foreach (var next in nextSteps)
            {
                if (!inDegree.ContainsKey(next))
                    continue;

                var newDepth = depth[current] + 1;
                if (!depth.ContainsKey(next) || newDepth > depth[next])
                    depth[next] = newDepth;

                inDegree[next]--;
                if (inDegree[next] == 0)
                    queue.Enqueue(next);
            }
        }

        if (depth.Count == 0)
            return Array.Empty<IReadOnlyList<string>>();

        var maxDepth = depth.Values.Max();
        var groups = new List<IReadOnlyList<string>>();

        for (var d = 0; d <= maxDepth; d++)
        {
            var group = depth
                .Where(kv => kv.Value == d)
                .Select(kv => kv.Key)
                .OrderBy(n => n)
                .ToList();

            if (group.Count > 0)
                groups.Add(group);
        }

        return groups;
    }

    public WorkflowGraphResult BuildExecutionGraph(WorkflowGraphCommand command)
    {
        if (command.WorkflowSteps.Count == 0)
            return WorkflowGraphResult.Fail("Workflow must contain at least one step.");

        var nodes = new List<WorkflowNode>();
        var stepIds = new HashSet<string>();

        foreach (var step in command.WorkflowSteps)
        {
            if (!stepIds.Add(step.StepId))
                return WorkflowGraphResult.Fail($"Duplicate step ID: '{step.StepId}'.");

            nodes.Add(new WorkflowNode(step.StepId, step.StepId, step.StepName, step.EngineName));
        }

        var edges = new List<WorkflowEdge>();
        foreach (var step in command.WorkflowSteps)
        {
            foreach (var dep in step.Dependencies)
            {
                if (!stepIds.Contains(dep))
                    return WorkflowGraphResult.Fail($"Step '{step.StepId}' references undefined dependency '{dep}'.");

                edges.Add(new WorkflowEdge(dep, step.StepId));
            }
        }

        var transitions = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var step in command.WorkflowSteps)
            transitions[step.StepId] = new List<string>();

        foreach (var edge in edges)
        {
            var list = (List<string>)transitions[edge.FromNode];
            list.Add(edge.ToNode);
        }

        var graph = new WorkflowGraph(command.WorkflowId, transitions);

        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();
        foreach (var node in graph.Transitions.Keys)
        {
            if (DetectCycleDfs(node, graph, visited, recStack))
                return WorkflowGraphResult.Fail("Circular dependency detected in workflow graph.");
        }

        var executionOrder = ComputeExecutionOrder(graph);
        var parallelGroups = ComputeParallelGroups(graph);

        var executionGraph = new WorkflowExecutionGraph(
            command.WorkflowId,
            nodes,
            edges,
            executionOrder,
            parallelGroups,
            DateTimeOffset.UtcNow);

        return WorkflowGraphResult.Ok(executionGraph);
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
