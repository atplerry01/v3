namespace Whycespace.WorkerPoolRuntime.Tests;

using Whycespace.WorkerPoolRuntime.Scaling;

public class WorkerScalingPolicyTests
{
    [Fact]
    public void Policy_StoresMinimumWorkers()
    {
        var policy = new WorkerScalingPolicy(2, 16);
        Assert.Equal(2, policy.MinimumWorkers);
    }

    [Fact]
    public void Policy_StoresMaximumWorkers()
    {
        var policy = new WorkerScalingPolicy(2, 16);
        Assert.Equal(16, policy.MaximumWorkers);
    }

    [Fact]
    public void Policy_SingleWorkerRange()
    {
        var policy = new WorkerScalingPolicy(1, 1);
        Assert.Equal(1, policy.MinimumWorkers);
        Assert.Equal(1, policy.MaximumWorkers);
    }
}
