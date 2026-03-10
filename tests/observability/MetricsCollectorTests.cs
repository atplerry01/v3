using Whycespace.Observability.Metrics;

namespace Whycespace.Observability.Tests;

public class MetricsCollectorTests
{
    [Fact]
    public void Increment_CreatesMetric_WhenNotExists()
    {
        var collector = new MetricsCollector();

        collector.Increment("WorkflowExecutions");

        Assert.Equal(1, collector.Get("WorkflowExecutions"));
    }

    [Fact]
    public void Increment_IncrementsExistingMetric()
    {
        var collector = new MetricsCollector();

        collector.Increment("EngineInvocations");
        collector.Increment("EngineInvocations");
        collector.Increment("EngineInvocations");

        Assert.Equal(3, collector.Get("EngineInvocations"));
    }

    [Fact]
    public void Get_ReturnsZero_WhenMetricNotExists()
    {
        var collector = new MetricsCollector();

        Assert.Equal(0, collector.Get("NonExistent"));
    }

    [Fact]
    public void GetAll_ReturnsAllMetrics()
    {
        var collector = new MetricsCollector();

        collector.Increment("WorkflowExecutions");
        collector.Increment("EngineInvocations");
        collector.Increment("EngineInvocations");

        var all = collector.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Equal(1, all["WorkflowExecutions"]);
        Assert.Equal(2, all["EngineInvocations"]);
    }
}
