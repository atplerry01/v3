namespace Whycespace.RuntimeValidation.Scenarios;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T4A.API;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi;
using Whycespace.Engines.T2E.Core.Revenue;
using Whycespace.Engines.T2E.Core.Capital;
using Whycespace.RuntimeValidation.Models;
using Whycespace.RuntimeValidation.Reports;
using System.Diagnostics;

public sealed class TaxiRideScenario
{
    public static readonly ValidationScenario Definition = new(
        Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001"),
        "TaxiRideScenario",
        "WhyceMobility",
        "End-to-end taxi ride: API → Command → Workflow → Engines → Events → Projections → Economic"
    );

    public async Task<ValidationReport> ExecuteAsync()
    {
        var sw = Stopwatch.StartNew();
        var steps = new List<string>();
        var workflowId = Guid.NewGuid().ToString();

        try
        {
            // Step 1: API Engine accepts the ride request
            var apiEngine = new APIEngine();
            var apiContext = new EngineContext(
                Guid.NewGuid(), workflowId, "DispatchCommand",
                "partition-mobility", new Dictionary<string, object>
                {
                    ["apiAction"] = "ride.request",
                    ["userId"] = "user-taxi-1",
                    ["pickupLatitude"] = 51.5074,
                    ["pickupLongitude"] = -0.1278,
                    ["dropoffLatitude"] = 51.5155,
                    ["dropoffLongitude"] = -0.1410
                });

            var apiResult = await apiEngine.ExecuteAsync(apiContext);
            if (!apiResult.Success) return Fail(sw, steps, "API Engine failed: " + apiResult.Output.GetValueOrDefault("error"));
            steps.Add("APIEngine → APICommandAccepted");

            // Step 2: Ride Execution — ValidateRequest
            var rideEngine = new RideExecutionEngine();
            var validateContext = new EngineContext(
                Guid.NewGuid(), workflowId, "ValidateRequest",
                "partition-mobility", new Dictionary<string, object>
                {
                    ["pickupLatitude"] = 51.5074,
                    ["pickupLongitude"] = -0.1278,
                    ["dropoffLatitude"] = 51.5155,
                    ["dropoffLongitude"] = -0.1410
                });

            var validateResult = await rideEngine.ExecuteAsync(validateContext);
            if (!validateResult.Success) return Fail(sw, steps, "RideExecution.ValidateRequest failed");
            steps.Add("RideExecutionEngine → RideRequestValidated");

            // Step 3: Ride Execution — AssignDriver
            var assignContext = new EngineContext(
                Guid.NewGuid(), workflowId, "AssignDriver",
                "partition-mobility", new Dictionary<string, object>
                {
                    ["assignedDriverId"] = Guid.NewGuid().ToString()
                });

            var assignResult = await rideEngine.ExecuteAsync(assignContext);
            if (!assignResult.Success) return Fail(sw, steps, "RideExecution.AssignDriver failed");
            steps.Add("RideExecutionEngine → DriverAssigned");

            // Step 4: Ride Execution — CompleteTrip
            var completeContext = new EngineContext(
                Guid.NewGuid(), workflowId, "CompleteTrip",
                "partition-mobility", new Dictionary<string, object>
                {
                    ["fare"] = 25.50m
                });

            var completeResult = await rideEngine.ExecuteAsync(completeContext);
            if (!completeResult.Success) return Fail(sw, steps, "RideExecution.CompleteTrip failed");
            steps.Add("RideExecutionEngine → TripCompleted");

            // Step 5: Revenue Recording
            var revenueEngine = new RevenueRecordingEngine();
            var revenueContext = new EngineContext(
                Guid.NewGuid(), workflowId, "RecordRevenue",
                "partition-mobility", new Dictionary<string, object>
                {
                    ["spvId"] = Guid.NewGuid().ToString(),
                    ["assetId"] = Guid.NewGuid().ToString(),
                    ["amount"] = 25.50m,
                    ["source"] = "TaxiRide",
                    ["period"] = "2026-Q1"
                });

            var revenueResult = await revenueEngine.ExecuteAsync(revenueContext);
            if (!revenueResult.Success) return Fail(sw, steps, "RevenueRecording failed");
            steps.Add("RevenueRecordingEngine → RevenueRecorded");

            // Step 6: Profit Distribution
            var profitEngine = new ProfitDistributionEngine();
            var profitContext = new EngineContext(
                Guid.NewGuid(), workflowId, "DistributeProfit",
                "partition-mobility", new Dictionary<string, object>
                {
                    ["spvId"] = Guid.NewGuid().ToString(),
                    ["vaultId"] = Guid.NewGuid().ToString(),
                    ["totalRevenue"] = 25.50m,
                    ["totalCosts"] = 10.00m,
                    ["distributionRate"] = 0.7m
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
