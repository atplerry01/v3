namespace Whycespace.WorkerPoolRuntime.Scaling;

public sealed class WorkerScalingPolicy
{
    public int MinimumWorkers { get; }

    public int MaximumWorkers { get; }

    public WorkerScalingPolicy(int minimumWorkers, int maximumWorkers)
    {
        MinimumWorkers = minimumWorkers;
        MaximumWorkers = maximumWorkers;
    }
}
