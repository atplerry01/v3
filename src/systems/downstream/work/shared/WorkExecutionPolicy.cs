namespace Whycespace.Systems.Downstream.Work.Shared;

public sealed class WorkExecutionPolicy
{
    public bool CanAssignTask(string workerId, string taskType, string clusterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskType);
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);

        return true;
    }

    public bool CanExecuteInCluster(string workerId, string clusterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);

        return true;
    }
}
