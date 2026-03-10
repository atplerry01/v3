namespace Whycespace.WorkflowRuntime.Context;

public sealed class WorkflowInstance
{
    public Guid WorkflowInstanceId { get; }
    public string WorkflowName { get; }
    public string PartitionKey { get; }
    public int CurrentStepIndex { get; set; }
    public IReadOnlyDictionary<string, object> Input { get; }

    public WorkflowInstance(
        Guid workflowInstanceId,
        string workflowName,
        string partitionKey,
        IReadOnlyDictionary<string, object> input)
    {
        WorkflowInstanceId = workflowInstanceId;
        WorkflowName = workflowName;
        PartitionKey = partitionKey;
        CurrentStepIndex = 0;
        Input = input;
    }
}
