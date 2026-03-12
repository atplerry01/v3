namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.Engines.T1M.WSS.Stores;

public sealed class WorkflowDefinitionEngine
{
    private readonly WorkflowDefinitionStore _store;

    public WorkflowDefinitionEngine(WorkflowDefinitionStore store)
    {
        _store = store;
    }

    public WorkflowDefinition RegisterWorkflowDefinition(string workflowId, string name, string description, string version, IReadOnlyList<WorkflowStep> steps)
    {
        var definition = new WorkflowDefinition(
            workflowId,
            name,
            description,
            version,
            steps,
            DateTimeOffset.UtcNow);

        _store.Register(definition);
        return definition;
    }

    public WorkflowDefinition GetWorkflowDefinition(string workflowId)
    {
        return _store.Get(workflowId);
    }

    public IReadOnlyCollection<WorkflowDefinition> ListWorkflowDefinitions()
    {
        return _store.GetAll();
    }

    public IReadOnlyList<string> ValidateWorkflowDefinition(WorkflowDefinition definition)
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
        var circularViolation = DetectCircularDependency(definition.Steps);
        if (circularViolation is not null)
            violations.Add(circularViolation);

        return violations;
    }

    private static string? DetectCircularDependency(IReadOnlyList<WorkflowStep> steps)
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
}
