namespace Whycespace.Engines.T4A_Access;

using Whycespace.Shared.Contracts;

public sealed class APIEngine : IEngine
{
    public string Name => "API";

    private static readonly IReadOnlyDictionary<string, string> CommandRoutes =
        new Dictionary<string, string>
        {
            // Mobility
            ["ride.request"] = "RequestRide",
            ["ride.cancel"] = "CancelRide",
            ["driver.register"] = "RegisterDriver",
            ["driver.update-location"] = "UpdateDriverLocation",

            // Property
            ["property.list"] = "ListProperty",
            ["property.withdraw"] = "WithdrawProperty",
            ["tenant.apply"] = "ApplyForProperty",

            // Economic
            ["vault.create"] = "CreateVault",
            ["capital.contribute"] = "ContributeCapital",
            ["spv.create"] = "CreateSpv",
            ["asset.register"] = "RegisterAsset",
            ["revenue.record"] = "RecordRevenue",
            ["profit.distribute"] = "DistributeProfit",

            // Governance
            ["policy.evaluate"] = "EvaluatePolicy",
            ["governance.authorize"] = "AuthorizeGovernance"
        };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var apiAction = context.Data.GetValueOrDefault("apiAction") as string;
        if (string.IsNullOrEmpty(apiAction))
            return Task.FromResult(EngineResult.Fail("Missing apiAction"));

        var apiVersion = context.Data.GetValueOrDefault("apiVersion") as string ?? "v1";
        var requestId = context.Data.GetValueOrDefault("requestId") as string ?? Guid.NewGuid().ToString();
        var userId = context.Data.GetValueOrDefault("userId") as string;

        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(EngineResult.Fail("Missing userId — all API requests require authentication"));

        if (!CommandRoutes.TryGetValue(apiAction, out var commandType))
            return Task.FromResult(EngineResult.Fail($"Unknown apiAction: {apiAction}"));

        var contentType = context.Data.GetValueOrDefault("contentType") as string ?? "application/json";
        if (contentType != "application/json")
            return Task.FromResult(EngineResult.Fail($"Unsupported contentType: {contentType}"));

        var payload = ExtractPayload(context.Data);
        var commandId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("APICommandAccepted", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["requestId"] = requestId,
                    ["commandId"] = commandId.ToString(),
                    ["commandType"] = commandType,
                    ["apiAction"] = apiAction,
                    ["apiVersion"] = apiVersion,
                    ["userId"] = userId,
                    ["payloadKeys"] = string.Join(",", payload.Keys),
                    ["topic"] = "whyce.commands"
                })
        };

        var output = new Dictionary<string, object>(payload)
        {
            ["requestId"] = requestId,
            ["commandId"] = commandId.ToString(),
            ["commandType"] = commandType,
            ["apiAction"] = apiAction,
            ["apiVersion"] = apiVersion,
            ["userId"] = userId,
            ["accepted"] = true
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Dictionary<string, object> ExtractPayload(IReadOnlyDictionary<string, object> data)
    {
        var reserved = new HashSet<string>
        {
            "apiAction", "apiVersion", "requestId", "userId", "contentType",
            "token", "correlationId"
        };

        var payload = new Dictionary<string, object>();
        foreach (var kvp in data)
        {
            if (!reserved.Contains(kvp.Key))
                payload[kvp.Key] = kvp.Value;
        }
        return payload;
    }
}
