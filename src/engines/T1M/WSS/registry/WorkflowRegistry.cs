namespace Whycespace.Engines.T1M.WSS.Registry;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Models;

public sealed class WorkflowRegistry : IWorkflowRegistry
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new();

    public void RegisterWorkflow(WorkflowDefinition workflow)
    {
        var violations = Validate(workflow);
        if (violations.Count > 0)
            throw new ArgumentException($"Invalid workflow: {string.Join("; ", violations)}");

        if (!_workflows.TryAdd(workflow.WorkflowId, workflow))
            throw new InvalidOperationException($"Workflow already registered: {workflow.WorkflowId}");
    }

    public WorkflowDefinition GetWorkflow(string workflowId)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
            throw new KeyNotFoundException($"Workflow not found: {workflowId}");
        return workflow;
    }

    public IReadOnlyCollection<WorkflowDefinition> ListWorkflows()
    {
        return _workflows.Values.ToList();
    }

    public bool WorkflowExists(string workflowId)
    {
        return _workflows.ContainsKey(workflowId);
    }

    public void RemoveWorkflow(string workflowId)
    {
        if (!_workflows.TryRemove(workflowId, out _))
            throw new KeyNotFoundException($"Workflow not found: {workflowId}");
    }

    private static IReadOnlyList<string> Validate(WorkflowDefinition definition)
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

        var stepIds = new HashSet<string>();
        foreach (var step in definition.Steps)
        {
            if (!stepIds.Add(step.StepId))
                violations.Add($"Duplicate step ID: '{step.StepId}'.");
        }

        foreach (var step in definition.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!stepIds.Contains(next))
                    violations.Add($"Step '{step.StepId}': NextStep '{next}' does not exist.");
            }
        }

        if (HasCircularDependency(definition.Steps))
            violations.Add("Circular dependency detected in workflow steps.");

        return violations;
    }

    private static bool HasCircularDependency(IReadOnlyList<WorkflowStep> steps)
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

        return sorted < steps.Count;
    }
}
