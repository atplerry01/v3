using Whycespace.ReliabilityRuntime.Timeout;

namespace Whycespace.ReliabilityRuntime.Tests;

public sealed class WorkflowTimeoutManagerTests
{
    [Fact]
    public void Start_TracksWorkflow()
    {
        var manager = new WorkflowTimeoutManager();

        manager.Start("wf-1");

        Assert.Equal(1, manager.TrackedCount);
    }

    [Fact]
    public void HasTimedOut_ReturnsFalse_WhenNotStarted()
    {
        var manager = new WorkflowTimeoutManager();

        Assert.False(manager.HasTimedOut("wf-1", TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void HasTimedOut_ReturnsFalse_WhenWithinTimeout()
    {
        var manager = new WorkflowTimeoutManager();
        manager.Start("wf-1");

        Assert.False(manager.HasTimedOut("wf-1", TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void HasTimedOut_ReturnsTrue_WhenExpired()
    {
        var manager = new WorkflowTimeoutManager();
        manager.Start("wf-1");

        Assert.True(manager.HasTimedOut("wf-1", TimeSpan.Zero));
    }
}
