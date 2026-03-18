using Whycespace.Engines.T0U.WhycePolicy.Monitoring.Engines;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyMonitoringTests
{
    private readonly PolicyMonitoringStore _store = new();
    private readonly PolicyMonitoringEngine _engine;

    public PolicyMonitoringTests()
    {
        _engine = new PolicyMonitoringEngine(_store);
    }

    [Fact]
    public void RecordEvaluation_CreatesMetrics()
    {
        _engine.RecordPolicyEvaluation("pol-1", "identity", true);

        var metrics = _engine.GetPolicyMetrics("pol-1");

        Assert.NotNull(metrics);
        Assert.Equal("pol-1", metrics.PolicyId);
        Assert.Equal("identity", metrics.Domain);
        Assert.Equal(1, metrics.Evaluations);
    }

    [Fact]
    public void RecordEvaluation_AllowedIncrementsAllowCount()
    {
        _engine.RecordPolicyEvaluation("pol-allow", "identity", true);

        var metrics = _engine.GetPolicyMetrics("pol-allow");

        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.AllowedCount);
        Assert.Equal(0, metrics.DeniedCount);
    }

    [Fact]
    public void RecordEvaluation_DeniedIncrementsDenyCount()
    {
        _engine.RecordPolicyEvaluation("pol-deny", "identity", false);

        var metrics = _engine.GetPolicyMetrics("pol-deny");

        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.AllowedCount);
        Assert.Equal(1, metrics.DeniedCount);
    }

    [Fact]
    public void GetPolicyMetrics_ReturnsCorrectData()
    {
        _engine.RecordPolicyEvaluation("pol-metrics", "clusters", true);
        _engine.RecordPolicyEvaluation("pol-metrics", "clusters", false);

        var metrics = _engine.GetPolicyMetrics("pol-metrics");

        Assert.NotNull(metrics);
        Assert.Equal(2, metrics.Evaluations);
        Assert.Equal(1, metrics.AllowedCount);
        Assert.Equal(1, metrics.DeniedCount);
    }

    [Fact]
    public void RecordEvaluation_MultipleEvaluationsAccumulate()
    {
        _engine.RecordPolicyEvaluation("pol-accum", "identity", true);
        _engine.RecordPolicyEvaluation("pol-accum", "identity", true);
        _engine.RecordPolicyEvaluation("pol-accum", "identity", false);
        _engine.RecordPolicyEvaluation("pol-accum", "identity", true);
        _engine.RecordPolicyEvaluation("pol-accum", "identity", false);

        var metrics = _engine.GetPolicyMetrics("pol-accum");

        Assert.NotNull(metrics);
        Assert.Equal(5, metrics.Evaluations);
        Assert.Equal(3, metrics.AllowedCount);
        Assert.Equal(2, metrics.DeniedCount);
    }

    [Fact]
    public void GetAllMetrics_MultiplePoliciesTrackedIndependently()
    {
        _engine.RecordPolicyEvaluation("pol-a", "identity", true);
        _engine.RecordPolicyEvaluation("pol-b", "clusters", false);
        _engine.RecordPolicyEvaluation("pol-c", "economic", true);

        var all = _engine.GetAllMetrics();

        Assert.Equal(3, all.Count);
        Assert.Contains(all, m => m.PolicyId == "pol-a" && m.AllowedCount == 1);
        Assert.Contains(all, m => m.PolicyId == "pol-b" && m.DeniedCount == 1);
        Assert.Contains(all, m => m.PolicyId == "pol-c" && m.AllowedCount == 1);
    }

    [Fact]
    public void RecordEvaluation_ConcurrentUpdates_ThreadSafe()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
        {
            var allowed = i % 2 == 0;
            tasks.Add(Task.Run(() => _engine.RecordPolicyEvaluation("pol-concurrent", "identity", allowed)));
        }

        Task.WaitAll(tasks.ToArray());

        var metrics = _engine.GetPolicyMetrics("pol-concurrent");

        Assert.NotNull(metrics);
        Assert.Equal(100, metrics.Evaluations);
        Assert.Equal(50, metrics.AllowedCount);
        Assert.Equal(50, metrics.DeniedCount);
    }
}
