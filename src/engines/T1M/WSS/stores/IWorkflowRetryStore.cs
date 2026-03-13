namespace Whycespace.Engines.T1M.WSS.Stores;

public interface IWorkflowRetryStore
{
    int GetRetryCount(string instanceId, string stepId);
    int IncrementRetryCount(string instanceId, string stepId);
    void ResetRetryCount(string instanceId, string stepId);
}
