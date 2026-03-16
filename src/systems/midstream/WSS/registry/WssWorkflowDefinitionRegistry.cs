namespace Whycespace.Systems.WSS.Registry;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Workflows;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WssWorkflowDefinitionRegistry : IWssWorkflowDefinitionRegistry
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows = new();

    public void RegisterWorkflow(WorkflowDefinition workflow)
    {
        Validate(workflow);

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

    private static void Validate(WorkflowDefinition workflow)
    {
        if (string.IsNullOrWhiteSpace(workflow.WorkflowId))
            throw new ArgumentException("WorkflowId must not be empty.");

        if (string.IsNullOrWhiteSpace(workflow.Name))
            throw new ArgumentException("Workflow name must not be empty.");

        if (workflow.Steps.Count == 0)
            throw new ArgumentException("Workflow must have at least one step.");

        var stepIds = new HashSet<string>();
        foreach (var step in workflow.Steps)
        {
            if (!stepIds.Add(step.StepId))
                throw new ArgumentException($"Duplicate step ID: {step.StepId}");
        }

        // Validate next step references
        foreach (var step in workflow.Steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (!stepIds.Contains(next))
                    throw new ArgumentException($"Step '{step.StepId}' references non-existent step '{next}'.");
            }
        }

        // Check for circular dependencies
        if (HasCircularDependency(workflow.Steps))
            throw new ArgumentException("Circular dependency detected in workflow steps.");
    }

    private static bool HasCircularDependency(IReadOnlyList<WorkflowStep> steps)
    {
        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var step in steps)
        {
            inDegree[step.StepId] = 0;
            adjacency[step.StepId] = new List<string>();
        }

        foreach (var step in steps)
        {
            foreach (var next in step.NextSteps)
            {
                if (adjacency.ContainsKey(step.StepId) && inDegree.ContainsKey(next))
                {
                    adjacency[step.StepId].Add(next);
                    inDegree[next]++;
                }
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var visited = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited++;

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        return visited != steps.Count;
    }
}
