using Whycespace.Contracts.Primitives;

namespace Whycespace.Reliability.Models;

public sealed class WorkflowStateEntry
{
    public Guid WorkflowInstanceId { get; }
    public string WorkflowName { get; }
    public int CurrentStepIndex { get; set; }
    public PartitionKey PartitionKey { get; }
    public IReadOnlyDictionary<string, object> WorkflowContext { get; }
    public DateTime LastUpdated { get; set; }

    public WorkflowStateEntry(
        Guid workflowInstanceId,
        string workflowName,
        int currentStepIndex,
        PartitionKey partitionKey,
        IReadOnlyDictionary<string, object> workflowContext)
    {
        WorkflowInstanceId = workflowInstanceId;
        WorkflowName = workflowName;
        CurrentStepIndex = currentStepIndex;
        PartitionKey = partitionKey;
        WorkflowContext = workflowContext;
        LastUpdated = DateTime.UtcNow;
    }
}
