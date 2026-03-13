namespace Whycespace.SystemValidation.Tests;

using Whycespace.RuntimeValidation.Scenarios;

public sealed class WorkflowExecutionTests
{
    [Fact]
    public async Task TaxiRideScenario_ExecutesEndToEnd()
    {
        var scenario = new TaxiRideScenario();
        var report = await scenario.ExecuteAsync();

        Assert.True(report.Success, report.Errors ?? "Unknown failure");
        Assert.Equal("TaxiRideScenario", report.ScenarioName);
        Assert.True(report.Steps.Count >= 6, $"Expected at least 6 steps, got {report.Steps.Count}");
        Assert.Contains(report.Steps, s => s.Contains("APIEngine"));
        Assert.Contains(report.Steps, s => s.Contains("RevenueRecorded"));
        Assert.Contains(report.Steps, s => s.Contains("ProfitDistributed"));
    }

    [Fact]
    public async Task PropertyLettingScenario_ExecutesEndToEnd()
    {
        var scenario = new PropertyLettingScenario();
        var report = await scenario.ExecuteAsync();

        Assert.True(report.Success, report.Errors ?? "Unknown failure");
        Assert.Equal("PropertyLettingScenario", report.ScenarioName);
        Assert.True(report.Steps.Count >= 6, $"Expected at least 6 steps, got {report.Steps.Count}");
        Assert.Contains(report.Steps, s => s.Contains("APIEngine"));
        Assert.Contains(report.Steps, s => s.Contains("ListingPublished"));
        Assert.Contains(report.Steps, s => s.Contains("ProfitDistributed"));
    }
}
