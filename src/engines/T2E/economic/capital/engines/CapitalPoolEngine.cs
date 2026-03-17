namespace Whycespace.Engines.T2E.Economic.Capital.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("CapitalPool", EngineTier.T2E, EngineKind.Mutation, "CapitalPoolCommand", typeof(EngineEvent))]
public sealed class CapitalPoolEngine : IEngine
{
    public string Name => "CapitalPool";

    private static readonly string[] ValidCurrencies =
        { "GBP", "USD", "EUR", "CHF", "JPY", "AUD", "CAD", "SGD", "HKD", "NZD" };

    private static readonly string[] ValidActions =
        { "Create", "Activate", "Suspend", "Close" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string ?? "Create";

        return action switch
        {
            "Create" => HandleCreate(context),
            "Activate" => HandleActivate(context),
            "Suspend" => HandleSuspend(context),
            "Close" => HandleClose(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action: {action}. Supported: {string.Join(", ", ValidActions)}"))
        };
    }

    private Task<EngineResult> HandleCreate(EngineContext context)
    {
        // --- Validate PoolId ---
        var poolIdRaw = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolIdRaw, out var poolId))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        // --- Validate PoolName ---
        var poolName = context.Data.GetValueOrDefault("poolName") as string;
        if (string.IsNullOrEmpty(poolName))
            return Task.FromResult(EngineResult.Fail("Missing poolName"));

        // --- Validate ClusterId ---
        var clusterIdRaw = context.Data.GetValueOrDefault("clusterId") as string;
        if (string.IsNullOrEmpty(clusterIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing clusterId"));
        if (!Guid.TryParse(clusterIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid clusterId format"));

        // --- Validate SubClusterId ---
        var subClusterIdRaw = context.Data.GetValueOrDefault("subClusterId") as string;
        if (string.IsNullOrEmpty(subClusterIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing subClusterId"));
        if (!Guid.TryParse(subClusterIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid subClusterId format"));

        // --- Validate SPVId ---
        var spvIdRaw = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));
        if (!Guid.TryParse(spvIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid spvId format"));

        // --- Validate Currency ---
        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(ValidCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Invalid currency: {currency}. Supported: {string.Join(", ", ValidCurrencies)}"));

        // --- Validate CreatedBy ---
        var createdByRaw = context.Data.GetValueOrDefault("createdBy") as string;
        if (string.IsNullOrEmpty(createdByRaw))
            return Task.FromResult(EngineResult.Fail("Missing createdBy"));
        if (!Guid.TryParse(createdByRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid createdBy format"));

        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalPoolCreated", poolId,
                new Dictionary<string, object>
                {
                    ["poolId"] = poolIdRaw,
                    ["poolName"] = poolName,
                    ["clusterId"] = clusterIdRaw,
                    ["subClusterId"] = subClusterIdRaw,
                    ["spvId"] = spvIdRaw,
                    ["currency"] = currency,
                    ["createdBy"] = createdByRaw,
                    ["poolStatus"] = "Created",
                    ["createdAt"] = timestamp.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["poolId"] = poolIdRaw,
            ["poolName"] = poolName,
            ["poolStatus"] = "Created",
            ["timestamp"] = timestamp.ToString("O"),
            ["message"] = $"Capital pool '{poolName}' created successfully"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Task<EngineResult> HandleActivate(EngineContext context)
    {
        // --- Validate PoolId ---
        var poolIdRaw = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolIdRaw, out var poolId))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        // --- Validate current status ---
        var currentStatus = context.Data.GetValueOrDefault("currentPoolStatus") as string;
        if (currentStatus != "Created" && currentStatus != "Suspended")
            return Task.FromResult(EngineResult.Fail("Only pools with status Created or Suspended can be activated"));

        // --- Validate ActivatedBy ---
        var activatedByRaw = context.Data.GetValueOrDefault("activatedBy") as string;
        if (string.IsNullOrEmpty(activatedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing activatedBy"));
        if (!Guid.TryParse(activatedByRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid activatedBy format"));

        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalPoolActivated", poolId,
                new Dictionary<string, object>
                {
                    ["poolId"] = poolIdRaw,
                    ["activatedBy"] = activatedByRaw,
                    ["poolStatus"] = "Active",
                    ["activatedAt"] = timestamp.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["poolId"] = poolIdRaw,
            ["poolStatus"] = "Active",
            ["timestamp"] = timestamp.ToString("O"),
            ["message"] = "Capital pool activated successfully"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Task<EngineResult> HandleSuspend(EngineContext context)
    {
        // --- Validate PoolId ---
        var poolIdRaw = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolIdRaw, out var poolId))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        // --- Validate current status ---
        var currentStatus = context.Data.GetValueOrDefault("currentPoolStatus") as string;
        if (currentStatus != "Active")
            return Task.FromResult(EngineResult.Fail("Only Active pools can be suspended"));

        // --- Validate Reason ---
        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing suspension reason"));

        // --- Validate SuspendedBy ---
        var suspendedByRaw = context.Data.GetValueOrDefault("suspendedBy") as string;
        if (string.IsNullOrEmpty(suspendedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing suspendedBy"));
        if (!Guid.TryParse(suspendedByRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid suspendedBy format"));

        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalPoolSuspended", poolId,
                new Dictionary<string, object>
                {
                    ["poolId"] = poolIdRaw,
                    ["reason"] = reason,
                    ["suspendedBy"] = suspendedByRaw,
                    ["poolStatus"] = "Suspended",
                    ["suspendedAt"] = timestamp.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["poolId"] = poolIdRaw,
            ["poolStatus"] = "Suspended",
            ["timestamp"] = timestamp.ToString("O"),
            ["message"] = $"Capital pool suspended: {reason}"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Task<EngineResult> HandleClose(EngineContext context)
    {
        // --- Validate PoolId ---
        var poolIdRaw = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolIdRaw, out var poolId))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        // --- Validate current status ---
        var currentStatus = context.Data.GetValueOrDefault("currentPoolStatus") as string;
        if (currentStatus == "Closed")
            return Task.FromResult(EngineResult.Fail("Pool is already closed"));

        // --- Validate ClosedBy ---
        var closedByRaw = context.Data.GetValueOrDefault("closedBy") as string;
        if (string.IsNullOrEmpty(closedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing closedBy"));
        if (!Guid.TryParse(closedByRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid closedBy format"));

        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalPoolClosed", poolId,
                new Dictionary<string, object>
                {
                    ["poolId"] = poolIdRaw,
                    ["closedBy"] = closedByRaw,
                    ["poolStatus"] = "Closed",
                    ["closedAt"] = timestamp.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["poolId"] = poolIdRaw,
            ["poolStatus"] = "Closed",
            ["timestamp"] = timestamp.ToString("O"),
            ["message"] = "Capital pool closed successfully"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }
}
