namespace Whycespace.Runtime.Persistence.Abstractions;

public interface IWorkflowRetryStore
{
    int GetRetryCount(string instanceId, string stepId);
    int IncrementRetryCount(string instanceId, string stepId);
    void ResetRetryCount(string instanceId, string stepId);
}
