namespace Whycespace.Simulation;

using Whycespace.Contracts.Workflows;
using Whycespace.Systems.Midstream.WSS.Workflows;

public sealed record SimulationConfig(
    string Name,
    int TotalWorkflows,
    int Workers,
    TimeSpan? Duration,
    double FaultRate
);

public static class SimulationScenario
{
    public static SimulationConfig Small(int workers = 10, double faultRate = 0) => new(
        "Small", 1_000, workers, null, faultRate);

    public static SimulationConfig Medium(int workers = 100, double faultRate = 0) => new(
        "Medium", 50_000, workers, null, faultRate);

    public static SimulationConfig Large(int workers = 1000, double faultRate = 0) => new(
        "Large", 1_000_000, workers, null, faultRate);

    public static SimulationConfig Custom(int total, int workers, TimeSpan? duration = null, double faultRate = 0) => new(
        "Custom", total, workers, duration, faultRate);
}

public sealed record WorkflowPayload(
    string WorkflowType,
    WorkflowGraph Graph,
    Dictionary<string, object> Context
);

public static class WorkloadGenerator
{
    private static readonly RideRequestWorkflow RideWorkflow = new();
    private static readonly PropertyListingWorkflow PropertyWorkflow = new();
    private static readonly EconomicLifecycleWorkflow EconomicWorkflow = new();

    private static readonly ThreadLocal<Random> Rng = new(() => new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));

    public static WorkflowPayload GenerateRandom()
    {
        var r = Rng.Value!;
        return (r.Next(3)) switch
        {
            0 => GenerateRideRequest(r),
            1 => GeneratePropertyListing(r),
            _ => GenerateEconomicLifecycle(r)
        };
    }

    private static WorkflowPayload GenerateRideRequest(Random r)
    {
        var graph = RideWorkflow.BuildGraph();
        var ctx = new Dictionary<string, object>
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["pickupLatitude"] = 51.4 + r.NextDouble() * 0.3,
            ["pickupLongitude"] = -0.3 + r.NextDouble() * 0.4,
            ["dropoffLatitude"] = 51.4 + r.NextDouble() * 0.3,
            ["dropoffLongitude"] = -0.3 + r.NextDouble() * 0.4
        };
        return new WorkflowPayload("RideRequest", graph, ctx);
    }

    private static WorkflowPayload GeneratePropertyListing(Random r)
    {
        var graph = PropertyWorkflow.BuildGraph();
        var titles = new[] { "Studio Flat", "1 Bed Flat", "2 Bed Flat", "3 Bed House", "Penthouse", "Detached House" };
        var ctx = new Dictionary<string, object>
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["title"] = titles[r.Next(titles.Length)],
            ["description"] = $"Property listing {Guid.NewGuid():N}",
            ["monthlyRent"] = (decimal)(500 + r.Next(5000))
        };
        return new WorkflowPayload("PropertyListing", graph, ctx);
    }

    private static WorkflowPayload GenerateEconomicLifecycle(Random r)
    {
        var graph = EconomicWorkflow.BuildGraph();
        var ctx = new Dictionary<string, object>
        {
            ["amount"] = (decimal)(1000 + r.Next(100000)),
            ["spvName"] = $"SPV-{Guid.NewGuid().ToString("N")[..8]}",
            ["currency"] = "GBP"
        };
        return new WorkflowPayload("EconomicLifecycle", graph, ctx);
    }
}
