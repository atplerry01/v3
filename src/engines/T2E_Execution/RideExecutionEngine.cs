namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Shared.Contracts;

public sealed class RideExecutionEngine : IEngine
{
    public string Name => "RideExecution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var step = context.WorkflowStep;

        return step switch
        {
            "ValidateRequest" => HandleValidateRequest(context),
            "AssignDriver" => HandleAssignDriver(context),
            "StartTrip" => HandleStartTrip(context),
            "CompleteTrip" => HandleCompleteTrip(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown step: {step}"))
        };
    }

    private static Task<EngineResult> HandleValidateRequest(EngineContext context)
    {
        var hasPickup = context.Data.ContainsKey("pickupLatitude");
        if (!hasPickup)
            return Task.FromResult(EngineResult.Fail("Missing pickup location"));

        var events = new[] { EngineEvent.Create("RideRequestValidated", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleAssignDriver(EngineContext context)
    {
        var driverId = context.Data.GetValueOrDefault("assignedDriverId") as string;
        if (string.IsNullOrEmpty(driverId))
            return Task.FromResult(EngineResult.Fail("No driver assigned"));

        var events = new[] { EngineEvent.Create("DriverAssigned", Guid.Parse(context.WorkflowId),
            new Dictionary<string, object> { ["driverId"] = driverId }) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleStartTrip(EngineContext context)
    {
        var events = new[] { EngineEvent.Create("TripStarted", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleCompleteTrip(EngineContext context)
    {
        var fare = context.Data.GetValueOrDefault("fare");
        var events = new[] { EngineEvent.Create("TripCompleted", Guid.Parse(context.WorkflowId),
            new Dictionary<string, object> { ["fare"] = fare ?? 0m }) };
        return Task.FromResult(EngineResult.Ok(events));
    }
}
