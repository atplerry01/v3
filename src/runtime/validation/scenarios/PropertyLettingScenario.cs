namespace Whycespace.RuntimeValidation.Scenarios;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T4A.API;
using Whycespace.Engines.T2E.Clusters.Property.Letting;
using Whycespace.Engines.T2E.Core.Revenue;
using Whycespace.Engines.T2E.Core.Capital;
using Whycespace.RuntimeValidation.Models;
using Whycespace.RuntimeValidation.Reports;
using System.Diagnostics;

public sealed class PropertyLettingScenario
{
    public static readonly ValidationScenario Definition = new(
        Guid.Parse("a1b2c3d4-0002-0002-0002-000000000002"),
        "PropertyLettingScenario",
        "WhyceProperty",
        "End-to-end property letting: API → Command → Workflow → Engines → Events → Projections → Economic"
    );

    public async Task<ValidationReport> ExecuteAsync()
    {
        var sw = Stopwatch.StartNew();
        var steps = new List<string>();
        var workflowId = Guid.NewGuid().ToString();

        try
        {
            // Step 1: API Engine accepts property listing
            var apiEngine = new APIEngine();
            var apiContext = new EngineContext(
                Guid.NewGuid(), workflowId, "DispatchCommand",
                "partition-property", new Dictionary<string, object>
                {
                    ["apiAction"] = "property.list",
                    ["userId"] = "owner-prop-1",
                    ["title"] = "2 Bed Flat London",
                    ["monthlyRent"] = 1500.00m
                });

            var apiResult = await apiEngine.ExecuteAsync(apiContext);
            if (!apiResult.Success) return Fail(sw, steps, "API Engine failed");
            steps.Add("APIEngine → APICommandAccepted");

            // Step 2: Property Execution — ValidateListing
            var propertyEngine = new PropertyExecutionEngine();
            var validateContext = new EngineContext(
                Guid.NewGuid(), workflowId, "ValidateListing",
                "partition-property", new Dictionary<string, object>
                {
                    ["title"] = "2 Bed Flat London",
                    ["monthlyRent"] = 1500.00m
                });

            var validateResult = await propertyEngine.ExecuteAsync(validateContext);
            if (!validateResult.Success) return Fail(sw, steps, "PropertyExecution.ValidateListing failed");
            steps.Add("PropertyExecutionEngine → ListingValidated");

            // Step 3: Property Execution — PublishListing
            var publishContext = new EngineContext(
                Guid.NewGuid(), workflowId, "PublishListing",
                "partition-property", new Dictionary<string, object>
                {
                    ["title"] = "2 Bed Flat London",
                    ["monthlyRent"] = 1500.00m
                });

            var publishResult = await propertyEngine.ExecuteAsync(publishContext);
            if (!publishResult.Success) return Fail(sw, steps, "PropertyExecution.PublishListing failed");
            steps.Add("PropertyExecutionEngine → ListingPublished");

            // Step 4: Property Execution — MatchTenant
            var matchContext = new EngineContext(
                Guid.NewGuid(), workflowId, "MatchTenant",
                "partition-property", new Dictionary<string, object>
                {
                    ["tenantId"] = Guid.NewGuid().ToString()
                });

            var matchResult = await propertyEngine.ExecuteAsync(matchContext);
            if (!matchResult.Success) return Fail(sw, steps, "PropertyExecution.MatchTenant failed");
            steps.Add("PropertyExecutionEngine → TenantMatched");

            // Step 5: Revenue Recording
            var revenueEngine = new RevenueRecordingEngine();
            var revenueContext = new EngineContext(
                Guid.NewGuid(), workflowId, "RecordRevenue",
                "partition-property", new Dictionary<string, object>
                {
                    ["spvId"] = Guid.NewGuid().ToString(),
                    ["assetId"] = Guid.NewGuid().ToString(),
                    ["amount"] = 1500.00m,
                    ["source"] = "PropertyLetting",
                    ["period"] = "2026-Q1"
                });

            var revenueResult = await revenueEngine.ExecuteAsync(revenueContext);
            if (!revenueResult.Success) return Fail(sw, steps, "RevenueRecording failed");
            steps.Add("RevenueRecordingEngine → RevenueRecorded");

            // Step 6: Profit Distribution
            var profitEngine = new ProfitDistributionEngine();
            var profitContext = new EngineContext(
                Guid.NewGuid(), workflowId, "DistributeProfit",
                "partition-property", new Dictionary<string, object>
                {
                    ["spvId"] = Guid.NewGuid().ToString(),
                    ["vaultId"] = Guid.NewGuid().ToString(),
                    ["totalRevenue"] = 1500.00m,
                    ["totalCosts"] = 300.00m,
                    ["distributionRate"] = 0.8m
                });

            var profitResult = await profitEngine.ExecuteAsync(profitContext);
            if (!profitResult.Success) return Fail(sw, steps, "ProfitDistribution failed");
            steps.Add("ProfitDistributionEngine → ProfitDistributed");

            sw.Stop();
            return new ValidationReport(Definition.ScenarioId, Definition.ScenarioName, true, sw.Elapsed, steps, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ValidationReport(Definition.ScenarioId, Definition.ScenarioName, false, sw.Elapsed, steps, ex.Message);
        }
    }

    private static ValidationReport Fail(Stopwatch sw, List<string> steps, string error)
    {
        sw.Stop();
        return new ValidationReport(Definition.ScenarioId, Definition.ScenarioName, false, sw.Elapsed, steps, error);
    }
}
