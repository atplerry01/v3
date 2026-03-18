namespace Whycespace.Engines.T1M.WSS.Graph;

public sealed class WorkflowDependencyResult
{
    public string WorkflowId { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies { get; }

    public IReadOnlyList<string> ExecutionOrder { get; }

    public IReadOnlyList<string> MissingDependencies { get; }

    public IReadOnlyList<string> CircularDependencies { get; }

    public IReadOnlyList<string> ExternalWorkflowDependencies { get; }

    public bool HasIssues => MissingDependencies.Count > 0 || CircularDependencies.Count > 0;

    public WorkflowDependencyResult(
        string workflowId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> dependencies,
        IReadOnlyList<string> executionOrder,
        IReadOnlyList<string> missingDependencies,
        IReadOnlyList<string> circularDependencies,
        IReadOnlyList<string> externalWorkflowDependencies)
    {
        WorkflowId = workflowId;
        Dependencies = dependencies;
        ExecutionOrder = executionOrder;
        MissingDependencies = missingDependencies;
        CircularDependencies = circularDependencies;
        ExternalWorkflowDependencies = externalWorkflowDependencies;
    }
}
