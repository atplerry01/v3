namespace Whycespace.WorkflowRuntime.Context;

using Whycespace.Shared.Primitives.Common;

public sealed class WorkflowInstance
{
    public Guid WorkflowInstanceId { get; }
    public string WorkflowName { get; }
    public PartitionKey PartitionKey { get; }
    public int CurrentStepIndex { get; set; }
    public IReadOnlyDictionary<string, object> Input { get; }

    public WorkflowInstance(
        Guid workflowInstanceId,
        string workflowName,
        PartitionKey partitionKey,
        IReadOnlyDictionary<string, object> input)
    {
        WorkflowInstanceId = workflowInstanceId;
        WorkflowName = workflowName;
        PartitionKey = partitionKey;
        CurrentStepIndex = 0;
        Input = input;
    }
}
