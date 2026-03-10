namespace Whycespace.Engines.T0U_Constitutional;

using Whycespace.Contracts.Engines;

public sealed class PolicyEvaluationEngine : IEngine
{
    public string Name => "PolicyEvaluation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var policyId = context.Data.GetValueOrDefault("policyId") as string;
        if (string.IsNullOrEmpty(policyId))
            return Task.FromResult(EngineResult.Fail("Missing policyId"));

        var contextData = context.Data.GetValueOrDefault("contextData") as string ?? "";
        var decision = EvaluatePolicy(policyId, context.Data);

        var output = new Dictionary<string, object>
        {
            ["policyDecision"] = decision ? "Approved" : "Rejected",
            ["policyId"] = policyId
        };

        if (!decision)
        {
            var events = new[]
            {
                EngineEvent.Create("PolicyRejected", Guid.Parse(context.WorkflowId),
                    new Dictionary<string, object> { ["policyId"] = policyId, ["decision"] = "Rejected" })
            };
            return Task.FromResult(EngineResult.Ok(events, output));
        }

        var approvedEvents = new[]
        {
            EngineEvent.Create("PolicyApproved", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["policyId"] = policyId, ["decision"] = "Approved" })
        };

        return Task.FromResult(EngineResult.Ok(approvedEvents, output));
    }

    private static bool EvaluatePolicy(string policyId, IReadOnlyDictionary<string, object> data)
    {
        return policyId switch
        {
            "transaction-limit" => EvaluateTransactionLimit(data),
            "identity-required" => data.ContainsKey("userId"),
            "cluster-access" => data.ContainsKey("clusterId") && data.ContainsKey("userId"),
            "spv-creation" => data.ContainsKey("capitalId") && data.ContainsKey("allocatedCapital"),
            "economic-transfer" => data.ContainsKey("sourceVaultId") && data.ContainsKey("amount"),
            _ => true
        };
    }

    private static bool EvaluateTransactionLimit(IReadOnlyDictionary<string, object> data)
    {
        if (!data.TryGetValue("amount", out var amountObj))
            return false;

        var amount = amountObj switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => (decimal)i,
            long l => (decimal)l,
            _ => decimal.MaxValue
        };

        var limit = data.TryGetValue("transactionLimit", out var limitObj) && limitObj is decimal l2
            ? l2
            : 1_000_000m;

        return amount <= limit;
    }
}
